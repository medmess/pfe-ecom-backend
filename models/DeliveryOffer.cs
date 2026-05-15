using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class DeliveryOffer
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DiscountPercent { get; set; }

    public DateTime? EndsAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DeliveryCompanyId { get; set; } = string.Empty;

    [ForeignKey(nameof(DeliveryCompanyId))]
    public ApplicationUser DeliveryCompany { get; set; } = null!;
}
