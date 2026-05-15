using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class DeliveryBranch
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Wilaya { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DeliveryCompanyId { get; set; } = string.Empty;

    [ForeignKey(nameof(DeliveryCompanyId))]
    public ApplicationUser DeliveryCompany { get; set; } = null!;
}
