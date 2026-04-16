using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManager.Users
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.AccountType,
                u.StoreName,
                u.Wilaya,
                u.Market,
                u.Address
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return BadRequest("Email is already used.");

        var admin = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            AccountType = "admin",
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(admin, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roleResult = await _userManager.AddToRoleAsync(admin, "Admin");

        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors.Select(e => e.Description));

        return Ok(new
        {
            message = "Admin created successfully."
        });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
            return BadRequest("Admin accounts cannot be deleted.");

        if (user.AccountType?.ToLower() != "customer" && user.AccountType?.ToLower() != "supplier")
            return BadRequest("Only customer or supplier accounts can be deleted.");

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new
        {
            message = "User deleted successfully."
        });
    }
}

public record CreateAdminRequest(
    string FullName,
    string Email,
    string Password
);