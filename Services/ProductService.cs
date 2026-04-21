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
        return await _context.Products
            .AsNoTracking()
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                Category = p.Category,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                Category = p.Category,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .FirstOrDefaultAsync();
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
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl ?? string.Empty,
            SupplierId = isAdmin ? null : userId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Category = product.Category,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateProductRequest request, string userId, bool isAdmin)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return false;

        if (!isAdmin && product.SupplierId != userId)
            return false;

        product.Name = request.Name ?? string.Empty;
        product.Description = request.Description ?? string.Empty;
        product.Brand = request.Brand ?? string.Empty;
        product.Category = request.Category ?? string.Empty;
        product.Price = request.Price;
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

        if (!isAdmin && product.SupplierId != userId)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
}