using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Data;
using pfe.ecom.api.Models;
using pfe.ecom.api.Services;

namespace pfe.ecom.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OrderService _orderService;

    public DeliveryController(AppDbContext context, UserManager<ApplicationUser> userManager, OrderService orderService)
    {
        _context = context;
        _userManager = userManager;
        _orderService = orderService;
    }

    [HttpGet("companies")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DeliveryCompanyDto>>> GetCompanies([FromQuery] string? wilaya = null)
    {
        var users = await _userManager.GetUsersInRoleAsync("Delivery");
        var ids = users.Select(u => u.Id).ToList();

        var branches = await _context.DeliveryBranches.AsNoTracking()
            .Where(b => ids.Contains(b.DeliveryCompanyId))
            .ToListAsync();
        var prices = await _context.DeliveryPrices.AsNoTracking()
            .Where(p => ids.Contains(p.DeliveryCompanyId))
            .ToListAsync();
        var offers = await _context.DeliveryOffers.AsNoTracking()
            .Where(o => ids.Contains(o.DeliveryCompanyId) && (o.EndsAt == null || o.EndsAt > DateTime.UtcNow))
            .ToListAsync();

        return Ok(users.Select(u => ToCompanyDto(u, branches, prices, offers, wilaya)).ToList());
    }

    [HttpGet("orders")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAssignedOrders()
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized();

        if (User.IsInRole("Admin"))
            return Ok(await _orderService.GetAllAsync());

        var serviceName = user.StoreName ?? user.FullName;
        return Ok(await _orderService.GetForDeliveryServiceAsync(serviceName));
    }

    [HttpGet("branches")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<IEnumerable<DeliveryBranchDto>>> GetBranches()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var query = _context.DeliveryBranches.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin")) query = query.Where(b => b.DeliveryCompanyId == userId);

        return Ok(await query.Select(b => new DeliveryBranchDto
        {
            Id = b.Id,
            Name = b.Name,
            Wilaya = b.Wilaya,
            Address = b.Address
        }).ToListAsync());
    }

    [HttpPost("branches")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<DeliveryBranchDto>> AddBranch([FromBody] UpsertDeliveryBranchRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var branch = new DeliveryBranch
        {
            Name = request.Name.Trim(),
            Wilaya = Normalize(request.Wilaya),
            Address = Normalize(request.Address),
            DeliveryCompanyId = userId
        };

        _context.DeliveryBranches.Add(branch);
        await _context.SaveChangesAsync();
        return Ok(new DeliveryBranchDto { Id = branch.Id, Name = branch.Name, Wilaya = branch.Wilaya, Address = branch.Address });
    }

    [HttpGet("prices")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<IEnumerable<DeliveryPriceDto>>> GetPrices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var query = _context.DeliveryPrices.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin")) query = query.Where(p => p.DeliveryCompanyId == userId);

        var prices = await query.ToListAsync();
        return Ok(prices.Select(ToPriceDto).ToList());
    }

    [HttpPost("prices")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<DeliveryPriceDto>> UpsertPrice([FromBody] UpsertDeliveryPriceRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var wilaya = request.Wilaya.Trim();
        var price = await _context.DeliveryPrices
            .FirstOrDefaultAsync(p => p.DeliveryCompanyId == userId && p.Wilaya == wilaya);

        if (price == null)
        {
            price = new DeliveryPrice { DeliveryCompanyId = userId, Wilaya = wilaya };
            _context.DeliveryPrices.Add(price);
        }

        price.AddressPrice = request.AddressPrice;
        price.OfficePrice = request.OfficePrice;
        await _context.SaveChangesAsync();
        return Ok(ToPriceDto(price));
    }

    [HttpGet("offers")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<IEnumerable<DeliveryOfferDto>>> GetOffers()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var query = _context.DeliveryOffers.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin")) query = query.Where(o => o.DeliveryCompanyId == userId);

        var offers = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(offers.Select(ToOfferDto).ToList());
    }

    [HttpPost("offers")]
    [Authorize(Roles = "Delivery,Admin")]
    public async Task<ActionResult<DeliveryOfferDto>> AddOffer([FromBody] UpsertDeliveryOfferRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var offer = new DeliveryOffer
        {
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            DiscountPercent = request.DiscountPercent,
            EndsAt = request.EndsAt,
            DeliveryCompanyId = userId
        };

        _context.DeliveryOffers.Add(offer);
        await _context.SaveChangesAsync();
        return Ok(ToOfferDto(offer));
    }

    private async Task<ApplicationUser?> CurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private static DeliveryCompanyDto ToCompanyDto(ApplicationUser user, List<DeliveryBranch> branches, List<DeliveryPrice> prices, List<DeliveryOffer> offers, string? wilaya)
    {
        var companyPrices = prices.Where(p => p.DeliveryCompanyId == user.Id).ToList();
        var selectedPrice = companyPrices.FirstOrDefault(p => !string.IsNullOrWhiteSpace(wilaya) && p.Wilaya.Equals(wilaya, StringComparison.OrdinalIgnoreCase))
            ?? companyPrices.FirstOrDefault();

        var activeOffers = offers.Where(o => o.DeliveryCompanyId == user.Id).Select(ToOfferDto).ToList();
        var discount = activeOffers.OrderByDescending(o => o.DiscountPercent).FirstOrDefault()?.DiscountPercent ?? 0;

        static decimal ApplyDiscount(decimal value, int discountPercent)
            => discountPercent <= 0 ? value : Math.Max(0, Math.Round(value * (100 - discountPercent) / 100, 2));

        return new DeliveryCompanyDto
        {
            Id = user.Id,
            Name = !string.IsNullOrWhiteSpace(user.StoreName) ? user.StoreName : user.FullName,
            Phone = user.StorePhone,
            Wilaya = user.Wilaya,
            Address = user.Address,
            LogoUrl = user.LogoUrl,
            AddressPrice = ApplyDiscount(selectedPrice?.AddressPrice ?? 500, discount),
            OfficePrice = ApplyDiscount(selectedPrice?.OfficePrice ?? 350, discount),
            Branches = branches.Where(b => b.DeliveryCompanyId == user.Id).Select(b => new DeliveryBranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Wilaya = b.Wilaya,
                Address = b.Address
            }).ToList(),
            Offers = activeOffers
        };
    }

    private static DeliveryPriceDto ToPriceDto(DeliveryPrice p) => new()
    {
        Id = p.Id,
        Wilaya = p.Wilaya,
        AddressPrice = p.AddressPrice,
        OfficePrice = p.OfficePrice
    };

    private static DeliveryOfferDto ToOfferDto(DeliveryOffer o) => new()
    {
        Id = o.Id,
        Title = o.Title,
        Description = o.Description,
        DiscountPercent = o.DiscountPercent,
        EndsAt = o.EndsAt
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
