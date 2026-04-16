using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public List<OrderItem> OrderItems { get; set; } = new();
}