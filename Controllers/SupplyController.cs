using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Data;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupplyController : ControllerBase
{
    private readonly AppDbContext _context;

    public SupplyController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("offers")]
    [Authorize(Roles = "Provider,Supplier,Dealer,Admin")]
    public async Task<ActionResult<IEnumerable<SupplierOfferDto>>> GetOffers()
    {
        var query = _context.SupplierOffers.AsQueryable();

        if (User.IsInRole("Provider"))
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            query = query.Where(o => o.ProviderId == providerId);
        }

        var offers = await query
            .AsNoTracking()
            .Include(o => o.Provider)
            .OrderBy(o => o.DeliveryDate)
            .ToListAsync();

        return Ok(offers.Select(ToOfferDto));
    }

    [HttpPost("offers")]
    [Authorize(Roles = "Provider,Admin")]
    public async Task<ActionResult<SupplierOfferDto>> CreateOffer([FromBody] CreateSupplierOfferRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(providerId))
            return Unauthorized();

        if (request.MinimumQuantity > request.AvailableQuantity)
            return BadRequest(new { message = "Minimum quantity cannot be greater than available quantity." });

        var provider = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == providerId);
        if (!User.IsInRole("Admin") && !CanUseCategory(provider?.SupplierCategories, request.Category))
            return BadRequest(new { message = "This category is not enabled for your supplier account." });

        var offer = new SupplierOffer
        {
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Description = request.Description?.Trim(),
            UnitPrice = request.UnitPrice,
            AvailableQuantity = request.AvailableQuantity,
            MinimumQuantity = request.MinimumQuantity,
            DeliveryDate = request.DeliveryDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.DeliveryDate, DateTimeKind.Utc)
                : request.DeliveryDate.ToUniversalTime(),
            ImageUrl = request.ImageUrl?.Trim(),
            ProviderId = providerId
        };

        _context.SupplierOffers.Add(offer);
        await _context.SaveChangesAsync();

        await _context.Entry(offer).Reference(o => o.Provider).LoadAsync();
        return CreatedAtAction(nameof(GetOffers), new { id = offer.Id }, ToOfferDto(offer));
    }

    [HttpPut("offers/{id}")]
    [Authorize(Roles = "Provider,Admin")]
    public async Task<IActionResult> UpdateOffer(int id, [FromBody] CreateSupplierOfferRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var offer = await _context.SupplierOffers.FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound(new { message = "Supplier product not found." });

        if (!User.IsInRole("Admin") && offer.ProviderId != userId)
            return Forbid();

        if (request.MinimumQuantity > request.AvailableQuantity)
            return BadRequest(new { message = "Minimum quantity cannot be greater than available quantity." });

        var provider = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (!User.IsInRole("Admin") && !CanUseCategory(provider?.SupplierCategories, request.Category))
            return BadRequest(new { message = "This category is not enabled for your supplier account." });

        offer.Name = request.Name.Trim();
        offer.Category = request.Category.Trim();
        offer.Description = request.Description?.Trim();
        offer.UnitPrice = request.UnitPrice;
        offer.AvailableQuantity = request.AvailableQuantity;
        offer.MinimumQuantity = request.MinimumQuantity;
        offer.DeliveryDate = request.DeliveryDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.DeliveryDate, DateTimeKind.Utc)
            : request.DeliveryDate.ToUniversalTime();
        offer.ImageUrl = request.ImageUrl?.Trim();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("offers/{id}")]
    [Authorize(Roles = "Provider,Admin")]
    public async Task<IActionResult> DeleteOffer(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var offer = await _context.SupplierOffers.FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound(new { message = "Supplier product not found." });

        if (!User.IsInRole("Admin") && offer.ProviderId != userId)
            return Forbid();

        _context.SupplierOffers.Remove(offer);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("requests")]
    [Authorize(Roles = "Provider,Supplier,Dealer,Admin")]
    public async Task<ActionResult<IEnumerable<SupplyRequestDto>>> GetRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _context.SupplyRequests
            .Include(r => r.SupplierOffer).ThenInclude(o => o.Provider)
            .Include(r => r.Dealer)
            .AsQueryable();

        if (User.IsInRole("Provider"))
            query = query.Where(r => r.SupplierOffer.ProviderId == userId);
        else if (!User.IsInRole("Admin"))
            query = query.Where(r => r.DealerId == userId);

        var requests = await query.AsNoTracking().OrderByDescending(r => r.RequestedAt).ToListAsync();
        return Ok(requests.Select(ToRequestDto));
    }

    [HttpPost("offers/{offerId}/requests")]
    [Authorize(Roles = "Supplier,Dealer,Admin")]
    public async Task<ActionResult<SupplyRequestDto>> CreateRequest(int offerId, [FromBody] CreateSupplyRequestRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dealerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(dealerId))
            return Unauthorized();

        var offer = await _context.SupplierOffers.Include(o => o.Provider).FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer == null)
            return NotFound(new { message = "Supplier product not found." });

        if (request.Quantity < offer.MinimumQuantity)
            return BadRequest(new { message = $"Minimum quantity for this product is {offer.MinimumQuantity}." });

        if (request.Quantity > offer.AvailableQuantity)
            return BadRequest(new { message = "Requested quantity is not available." });

        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        var supplyRequest = new SupplyRequest
        {
            SupplierOfferId = offer.Id,
            DealerId = dealerId,
            Quantity = request.Quantity,
            TotalAmount = request.Quantity * offer.UnitPrice,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentMethod == "Cash" ? "PendingCash" : "Paid",
            Status = paymentMethod == "Cash" ? "Pending" : "Paid",
            PaidAt = paymentMethod == "Cash" ? null : DateTime.UtcNow
        };

        offer.AvailableQuantity -= request.Quantity;
        _context.SupplyRequests.Add(supplyRequest);
        await _context.SaveChangesAsync();

        await _context.Entry(supplyRequest).Reference(r => r.Dealer).LoadAsync();
        supplyRequest.SupplierOffer = offer;
        return Ok(ToRequestDto(supplyRequest));
    }

    [HttpPut("requests/{id}/cancel")]
    [Authorize(Roles = "Supplier,Dealer,Admin")]
    public async Task<ActionResult<SupplyRequestDto>> CancelRequest(int id)
    {
        var dealerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var request = await _context.SupplyRequests
            .Include(r => r.SupplierOffer).ThenInclude(o => o.Provider)
            .Include(r => r.Dealer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound(new { message = "Supply request not found." });

        if (!User.IsInRole("Admin") && request.DealerId != dealerId)
            return Forbid();

        if (request.Status == "Cancelled")
            return Ok(ToRequestDto(request));

        request.Status = "Cancelled";
        request.PaymentStatus = request.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
        request.CancelledAt = DateTime.UtcNow;
        request.SupplierOffer.AvailableQuantity += request.Quantity;
        await _context.SaveChangesAsync();

        return Ok(ToRequestDto(request));
    }

    [HttpPut("requests/{id}/pay")]
    [Authorize(Roles = "Supplier,Dealer,Admin")]
    public async Task<ActionResult<SupplyRequestDto>> PayRequest(int id, [FromBody] PaySupplyRequestRequest request)
    {
        var dealerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var supplyRequest = await _context.SupplyRequests
            .Include(r => r.SupplierOffer).ThenInclude(o => o.Provider)
            .Include(r => r.Dealer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (supplyRequest == null)
            return NotFound(new { message = "Supply request not found." });

        if (!User.IsInRole("Admin") && supplyRequest.DealerId != dealerId)
            return Forbid();

        if (supplyRequest.Status == "Cancelled")
            return BadRequest(new { message = "Cancelled requests cannot be paid." });

        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        supplyRequest.PaymentMethod = paymentMethod;
        supplyRequest.PaymentStatus = paymentMethod == "Cash" ? "PendingCash" : "Paid";
        supplyRequest.Status = paymentMethod == "Cash" ? "Pending" : "Paid";
        supplyRequest.PaidAt = paymentMethod == "Cash" ? null : DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToRequestDto(supplyRequest));
    }

    private static string NormalizePaymentMethod(string? method)
    {
        var value = (method ?? "Cash").Trim().ToLowerInvariant();
        return value switch
        {
            "eddahabia" or "edahabia" => "Eddahabia",
            "ccp" => "CCP",
            _ => "Cash"
        };
    }

    private static SupplierOfferDto ToOfferDto(SupplierOffer offer)
    {
        return new SupplierOfferDto
        {
            Id = offer.Id,
            Name = offer.Name,
            Category = offer.Category,
            Description = offer.Description,
            UnitPrice = offer.UnitPrice,
            AvailableQuantity = offer.AvailableQuantity,
            MinimumQuantity = offer.MinimumQuantity,
            DeliveryDate = offer.DeliveryDate,
            HoursLeft = Math.Max(0, (offer.DeliveryDate - DateTime.UtcNow).TotalHours),
            ImageUrl = offer.ImageUrl,
            SupplierName = !string.IsNullOrWhiteSpace(offer.Provider.StoreName) ? offer.Provider.StoreName : offer.Provider.FullName,
            SupplierLogoUrl = offer.Provider.LogoUrl
        };
    }

    private static SupplyRequestDto ToRequestDto(SupplyRequest request)
    {
        return new SupplyRequestDto
        {
            Id = request.Id,
            SupplierOfferId = request.SupplierOfferId,
            ProductName = request.SupplierOffer.Name,
            Category = request.SupplierOffer.Category,
            Quantity = request.Quantity,
            TotalAmount = request.TotalAmount,
            Status = request.Status,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = request.PaymentStatus,
            RequestedAt = request.RequestedAt,
            DeliveryDate = request.SupplierOffer.DeliveryDate,
            DealerName = !string.IsNullOrWhiteSpace(request.Dealer.StoreName) ? request.Dealer.StoreName : request.Dealer.FullName,
            SupplierName = !string.IsNullOrWhiteSpace(request.SupplierOffer.Provider.StoreName) ? request.SupplierOffer.Provider.StoreName : request.SupplierOffer.Provider.FullName
        };
    }

    private static bool CanUseCategory(string? supplierCategories, string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        var allowed = (supplierCategories ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (allowed.Length == 0)
            return true;

        return allowed.Any(c => c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
