using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Models;
using pfe.ecom.api.Services;
// JWT Authentication Controller
namespace pfe.ecom.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwt,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<object>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest("FullName is required.");

            var normalizedType = (request.AccountType ?? "customer").Trim().ToLower();

            if (normalizedType != "customer" && normalizedType != "dealer" && normalizedType != "supplier" && normalizedType != "provider" && normalizedType != "delivery")
            {
                return BadRequest("AccountType must be customer, dealer, supplier, or delivery.");
            }

            var isDealerAccount = normalizedType == "dealer" || normalizedType == "supplier";
            var isProviderAccount = normalizedType == "provider";
            var isDeliveryAccount = normalizedType == "delivery";
            var supplierCategories = NormalizeSupplierCategories(request.SupplierCategories);

            if ((isDealerAccount || isProviderAccount || isDeliveryAccount) && string.IsNullOrWhiteSpace(request.StoreName))
            {
                return BadRequest("Company or store name is required.");
            }

            if (isProviderAccount && supplierCategories.Count == 0)
            {
                return BadRequest("Select at least one supplier category.");
            }

            if (supplierCategories.Count > 3)
            {
                return BadRequest("You can select up to 3 supplier categories.");
            }

            var email = request.Email.Trim().ToLower();

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                return BadRequest(new[] { "Email is already used." });
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = request.FullName.Trim(),
                AccountType = normalizedType,
                StoreName = (isDealerAccount || isProviderAccount || isDeliveryAccount) ? request.StoreName?.Trim() : null,
                StorePhone = (isDealerAccount || isProviderAccount || isDeliveryAccount) ? request.StorePhone?.Trim() : null,
                Wilaya = (isDealerAccount || isProviderAccount || isDeliveryAccount) ? request.Wilaya?.Trim() : null,
                Market = (isDealerAccount || isProviderAccount || isDeliveryAccount) ? request.Market?.Trim() : null,
                Address = request.Address?.Trim(),
                StoreDescription = (isDealerAccount || isProviderAccount || isDeliveryAccount) ? request.StoreDescription?.Trim() : null,
                LogoUrl = (isDealerAccount || isProviderAccount || isDeliveryAccount) ? request.LogoUrl?.Trim() : null,
                SupplierCategories = isProviderAccount ? string.Join(",", supplierCategories) : null,
                IsVerifiedSupplier = false,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors.Select(e => e.Description));
            }

            var roleName = normalizedType switch
            {
                "provider" => "Provider",
                "delivery" => "Delivery",
                "dealer" => "Dealer",
                "supplier" => "Supplier",
                _ => "Customer"
            };

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(roleResult.Errors.Select(e => e.Description));
            }

            return Ok(new
            {
                message = "User registered successfully. Please login.",
                email = user.Email,
                accountType = user.AccountType,
                role = roleName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during register for email: {Email}", request?.Email);

            return StatusCode(500, new
            {
                message = "An internal server error occurred during registration."
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            var email = request.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return Unauthorized("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized("Invalid credentials");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? user.AccountType;

            var token = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse(
                token,
                user.Email ?? string.Empty,
                user.FullName,
                role,
                user.AccountType,
                user.StoreName,
                user.StorePhone,
                user.Wilaya,
                user.Market,
                user.Address,
                user.StoreDescription,
                user.LogoUrl,
                user.SupplierCategories,
                user.IsVerifiedSupplier
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request?.Email);

            return StatusCode(500, new
            {
                message = "An internal server error occurred during login."
            });
        }
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<SupplierProfileResponse>> GetProfile()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound("User not found.");

            return Ok(new SupplierProfileResponse(
                user.Email ?? string.Empty,
                user.FullName,
                user.AccountType,
                user.StoreName,
                user.StorePhone,
                user.Wilaya,
                user.Market,
                user.Address,
                user.StoreDescription,
                user.LogoUrl,
                user.SupplierCategories,
                user.IsVerifiedSupplier
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading profile.");

            return StatusCode(500, new
            {
                message = "An internal server error occurred while loading profile."
            });
        }
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<SupplierProfileResponse>> UpdateProfile([FromBody] UpdateSupplierProfileRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound("User not found.");

            if (!string.Equals(user.AccountType, "supplier", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.AccountType, "dealer", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.AccountType, "provider", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only dealer and supplier accounts can update this profile.");
            }

            user.FullName = request.FullName.Trim();
            user.StoreName = request.StoreName.Trim();
            user.StorePhone = string.IsNullOrWhiteSpace(request.StorePhone) ? null : request.StorePhone.Trim();
            user.Wilaya = string.IsNullOrWhiteSpace(request.Wilaya) ? null : request.Wilaya.Trim();
            user.Market = string.IsNullOrWhiteSpace(request.Market) ? null : request.Market.Trim();
            user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
            user.StoreDescription = string.IsNullOrWhiteSpace(request.StoreDescription) ? null : request.StoreDescription.Trim();
            user.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();

            var supplierCategories = NormalizeSupplierCategories(request.SupplierCategories);
            if (string.Equals(user.AccountType, "provider", StringComparison.OrdinalIgnoreCase)
                && supplierCategories.Count > 0)
            {
                user.SupplierCategories = string.Join(",", supplierCategories);
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            return Ok(new SupplierProfileResponse(
                user.Email ?? string.Empty,
                user.FullName,
                user.AccountType,
                user.StoreName,
                user.StorePhone,
                user.Wilaya,
                user.Market,
                user.Address,
                user.StoreDescription,
                user.LogoUrl,
                user.SupplierCategories,
                user.IsVerifiedSupplier
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating dealer profile.");

            return StatusCode(500, new
            {
                message = "An internal server error occurred while updating profile."
            });
        }
    }

    [Authorize]
    [HttpPut("customer-profile")]
    public async Task<ActionResult<SupplierProfileResponse>> UpdateCustomerProfile([FromBody] UpdateCustomerProfileRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound("User not found.");

            if (!string.Equals(user.AccountType, "customer", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only customer accounts can update customer profile.");
            }

            user.FullName = request.FullName.Trim();
            user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            return Ok(new SupplierProfileResponse(
                user.Email ?? string.Empty,
                user.FullName,
                user.AccountType,
                user.StoreName,
                user.StorePhone,
                user.Wilaya,
                user.Market,
                user.Address,
                user.StoreDescription,
                user.LogoUrl,
                user.SupplierCategories,
                user.IsVerifiedSupplier
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating customer profile.");

            return StatusCode(500, new
            {
                message = "An internal server error occurred while updating customer profile."
            });
        }
    }

    private static List<string> NormalizeSupplierCategories(IEnumerable<string>? categories)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GPU",
            "CPU",
            "RAM",
            "Storage",
            "Motherboard",
            "PSU",
            "Case",
            "Cooling",
            "Monitor",
            "Keyboard",
            "Mouse",
            "Accessories"
        };

        return (categories ?? Array.Empty<string>())
            .Select(c => (c ?? string.Empty).Trim())
            .Where(c => allowed.Contains(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
