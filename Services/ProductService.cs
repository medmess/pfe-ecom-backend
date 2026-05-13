using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Data;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Services;

public class ProductService
{
  private readonly AppDbContext _context;

  public ProductService(AppDbContext context)
  {
    _context = context;
  }

  public async Task<List<ProductDto>> GetAllAsync()
  {
    return await BuildProductQuery(_context.Products)
        .ToListAsync();
  }

  public async Task<List<ProductDto>> GetMineAsync(string userId)
  {
    return await BuildProductQuery(
        _context.Products.Where(p => p.SupplierId == userId)
    ).ToListAsync();
  }

  public async Task<ProductDto?> GetByIdAsync(int id)
  {
    return await BuildProductQuery(
        _context.Products.Where(p => p.Id == id)
    ).FirstOrDefaultAsync();
  }

  public async Task<ProductDto?> CreateAsync(CreateProductRequest request, string userId, bool isAdmin)
  {
    var product = new Product
    {
      Name = request.Name ?? string.Empty,
      Description = request.Description ?? string.Empty,
      Brand = request.Brand ?? string.Empty,
      Category = request.Category ?? string.Empty,
      Price = request.Price,
      DiscountPercent = request.DiscountPercent,
      StockQuantity = request.StockQuantity,
      ImageUrl = request.ImageUrl ?? string.Empty,

      SupplierId = userId
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return await GetByIdAsync(product.Id);
  }

  public async Task<bool> UpdateAsync(int id, UpdateProductRequest request, string userId, bool isAdmin)
  {
    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
      return false;

    if (product.SupplierId != userId)
      return false;

    product.Name = request.Name ?? string.Empty;
    product.Description = request.Description ?? string.Empty;
    product.Brand = request.Brand ?? string.Empty;
    product.Category = request.Category ?? string.Empty;
    product.Price = request.Price;
    product.DiscountPercent = request.DiscountPercent;
    product.StockQuantity = request.StockQuantity;
    product.ImageUrl = request.ImageUrl ?? string.Empty;

    await _context.SaveChangesAsync();
    return true;
  }

  public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
  {
    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
      return false;

    if (product.SupplierId != userId)
      return false;

    _context.Products.Remove(product);
    await _context.SaveChangesAsync();
    return true;
  }

  private IQueryable<ProductDto> BuildProductQuery(IQueryable<Product> query)
  {
    return query
        .AsNoTracking()
        .Select(p => new ProductDto
        {
          Id = p.Id,
          Name = p.Name,
          Description = p.Description,
          Brand = p.Brand,
          Category = p.Category,
          Price = p.Price,
          DiscountPercent = p.DiscountPercent,
          StockQuantity = p.StockQuantity,
          ImageUrl = p.ImageUrl,
          StoreName = p.Supplier != null
              ? (!string.IsNullOrWhiteSpace(p.Supplier.StoreName) ? p.Supplier.StoreName : p.Supplier.FullName)
              : null,
          StoreLogoUrl = p.Supplier != null ? p.Supplier.LogoUrl : null
        });
  }
}
