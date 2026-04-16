namespace pfe.ecom.api.Contracts;

public class OrderDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<OrderItemDto> Items { get; set; } = new();
}