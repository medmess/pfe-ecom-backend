using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class SupplierOfferDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int AvailableQuantity { get; set; }
    public int MinimumQuantity { get; set; }
    public DateTime DeliveryDate { get; set; }
    public double HoursLeft { get; set; }
    public string? ImageUrl { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierLogoUrl { get; set; }
}

public class CreateSupplierOfferRequest
{
    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(2)]
    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int AvailableQuantity { get; set; }

    [Range(1, int.MaxValue)]
    public int MinimumQuantity { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string? ImageUrl { get; set; }
}

public class SupplyRequestDto
{
    public int Id { get; set; }
    public int SupplierOfferId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string? DealerName { get; set; }
    public string? SupplierName { get; set; }
}

public class CreateSupplyRequestRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public string PaymentMethod { get; set; } = "Cash";
}

public class PaySupplyRequestRequest
{
    public string PaymentMethod { get; set; } = "Cash";
}
