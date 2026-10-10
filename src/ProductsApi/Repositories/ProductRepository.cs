using Microsoft.EntityFrameworkCore;
using ProductsApi.Data;
using ProductsApi.Models;

namespace ProductsApi.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync() =>
        await _context.Products.AsNoTracking().OrderBy(product => product.Id).ToListAsync();

    public async Task<Product?> GetAsync(int id) =>
        await _context.Products.FindAsync(id);

    public async Task CreateAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await GetAsync(id);
        if (product is null)
            return;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}
