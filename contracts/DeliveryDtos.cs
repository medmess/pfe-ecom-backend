using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class DeliveryCompanyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Wilaya { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public decimal AddressPrice { get; set; }
    public decimal OfficePrice { get; set; }
    public List<DeliveryBranchDto> Branches { get; set; } = new();
    public List<DeliveryOfferDto> Offers { get; set; } = new();
}

public class DeliveryBranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Wilaya { get; set; }
    public string? Address { get; set; }
}

public class DeliveryPriceDto
{
    public int Id { get; set; }
    public string Wilaya { get; set; } = string.Empty;
    public decimal AddressPrice { get; set; }
    public decimal OfficePrice { get; set; }
}

public class DeliveryOfferDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DiscountPercent { get; set; }
    public DateTime? EndsAt { get; set; }
}

public class UpsertDeliveryBranchRequest
{
    [Required, MinLength(2)]
    public string Name { get; set; } = string.Empty;
    public string? Wilaya { get; set; }
    public string? Address { get; set; }
}

public class UpsertDeliveryPriceRequest
{
    [Required, MinLength(2)]
    public string Wilaya { get; set; } = string.Empty;
    [Range(0, double.MaxValue)]
    public decimal AddressPrice { get; set; }
    [Range(0, double.MaxValue)]
    public decimal OfficePrice { get; set; }
}

public class UpsertDeliveryOfferRequest
{
    [Required, MinLength(2)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Range(0, 100)]
    public int DiscountPercent { get; set; }
    public DateTime? EndsAt { get; set; }
}
