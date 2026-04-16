using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public record RegisterRequest(
    [Required, MinLength(3)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,

    string AccountType = "customer",

    string? StoreName = null,
    string? StorePhone = null,
    string? Wilaya = null,
    string? Market = null,
    string? Address = null,
    string? StoreDescription = null
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);

public record AuthResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    string AccountType,
    string? StoreName,
    bool IsVerifiedSupplier
);