using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}