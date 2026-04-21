using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Models;
using pfe.ecom.api.Services;

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

            if (normalizedType != "customer" && normalizedType != "supplier")
            {
                return BadRequest("AccountType must be either 'customer' or 'supplier'.");
            }

            if (normalizedType == "supplier" && string.IsNullOrWhiteSpace(request.StoreName))
            {
                return BadRequest("StoreName is required for supplier accounts.");
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
                StoreName = normalizedType == "supplier" ? request.StoreName?.Trim() : null,
                StorePhone = normalizedType == "supplier" ? request.StorePhone?.Trim() : null,
                Wilaya = normalizedType == "supplier" ? request.Wilaya?.Trim() : null,
                Market = normalizedType == "supplier" ? request.Market?.Trim() : null,
                Address = normalizedType == "supplier" ? request.Address?.Trim() : null,
                StoreDescription = normalizedType == "supplier" ? request.StoreDescription?.Trim() : null,
                LogoUrl = normalizedType == "supplier" ? request.LogoUrl?.Trim() : null,
                IsVerifiedSupplier = false,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors.Select(e => e.Description));
            }

            var roleName = normalizedType == "supplier" ? "Supplier" : "Customer";

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

            if (!string.Equals(user.AccountType, "supplier", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only supplier accounts can update supplier profile.");
            }

            user.FullName = request.FullName.Trim();
            user.StoreName = request.StoreName.Trim();
            user.StorePhone = string.IsNullOrWhiteSpace(request.StorePhone) ? null : request.StorePhone.Trim();
            user.Wilaya = string.IsNullOrWhiteSpace(request.Wilaya) ? null : request.Wilaya.Trim();
            user.Market = string.IsNullOrWhiteSpace(request.Market) ? null : request.Market.Trim();
            user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
            user.StoreDescription = string.IsNullOrWhiteSpace(request.StoreDescription) ? null : request.StoreDescription.Trim();
            user.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();

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
                user.IsVerifiedSupplier
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating supplier profile.");

            return StatusCode(500, new
            {
                message = "An internal server error occurred while updating profile."
            });
        }
    }
}