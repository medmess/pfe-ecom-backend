using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class CreateOrderRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be greater than 0.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}