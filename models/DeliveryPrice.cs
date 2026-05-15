using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class DeliveryPrice
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Wilaya { get; set; } = string.Empty;

    public decimal AddressPrice { get; set; }

    public decimal OfficePrice { get; set; }

    public string DeliveryCompanyId { get; set; } = string.Empty;

    [ForeignKey(nameof(DeliveryCompanyId))]
    public ApplicationUser DeliveryCompany { get; set; } = null!;
}
