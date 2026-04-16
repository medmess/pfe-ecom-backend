using Microsoft.AspNetCore.Identity;

namespace pfe.ecom.api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // customer or supplier or admin
    public string AccountType { get; set; } = "customer";

    // Supplier / store profile fields
    public string? StoreName { get; set; }
    public string? StorePhone { get; set; }
    public string? Wilaya { get; set; }
    public string? Market { get; set; }
    public string? Address { get; set; }
    public string? StoreDescription { get; set; }
    public bool IsVerifiedSupplier { get; set; } = false;
}