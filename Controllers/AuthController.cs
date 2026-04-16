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
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
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

            return Ok(new
            {
                message = "Login credentials are valid.",
                email = user.Email,
                fullName = user.FullName,
                role = role,
                accountType = user.AccountType
            });
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