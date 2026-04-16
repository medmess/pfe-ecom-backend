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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwt)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var normalizedType = (request.AccountType ?? "customer").Trim().ToLower();

        if (normalizedType != "customer" && normalizedType != "supplier")
        {
            return BadRequest("AccountType must be either 'customer' or 'supplier'.");
        }

        if (normalizedType == "supplier" && string.IsNullOrWhiteSpace(request.StoreName))
        {
            return BadRequest("StoreName is required for supplier accounts.");
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return BadRequest(new[] { "Email is already used." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            AccountType = normalizedType,
            StoreName = normalizedType == "supplier" ? request.StoreName : null,
            StorePhone = normalizedType == "supplier" ? request.StorePhone : null,
            Wilaya = normalizedType == "supplier" ? request.Wilaya : null,
            Market = normalizedType == "supplier" ? request.Market : null,
            Address = normalizedType == "supplier" ? request.Address : null,
            StoreDescription = normalizedType == "supplier" ? request.StoreDescription : null,
            IsVerifiedSupplier = false,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        var roleName = normalizedType == "supplier" ? "Supplier" : "Customer";
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors.Select(e => e.Description));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? roleName;
        var token = _jwt.CreateToken(user, roles);

        return Ok(new AuthResponse(
            token,
            user.Email ?? string.Empty,
            user.FullName,
            role,
            user.AccountType,
            user.StoreName,
            user.IsVerifiedSupplier
        ));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
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
            user.IsVerifiedSupplier
        ));
    }
}