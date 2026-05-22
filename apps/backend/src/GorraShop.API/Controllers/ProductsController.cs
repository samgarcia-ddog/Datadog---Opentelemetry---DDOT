using GorraShop.API.Data;
using GorraShop.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GorraShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db, ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        logger.LogInformation("Fetching products. Category={Category} Search={Search}", category, search);

        var query = db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category.Slug == category);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower())
                                  || p.Brand.ToLower().Contains(search.ToLower()));

        var total    = await query.CountAsync();
        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock,
                                        p.ImageUrl, p.Brand, p.Category.Name, p.CategoryId))
            .ToListAsync();

        return Ok(new { Total = total, Page = page, PageSize = pageSize, Items = products });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock,
                                        p.ImageUrl, p.Brand, p.Category.Name, p.CategoryId))
            .FirstOrDefaultAsync();

        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await db.Categories
            .Select(c => new { c.Id, c.Name, c.Slug, Count = c.Products.Count(p => p.IsActive) })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured()
    {
        // Devuelve 8 productos aleatorios (o los de mayor stock) como "featured"
        var featured = await db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Stock)
            .Take(8)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock,
                                        p.ImageUrl, p.Brand, p.Category.Name, p.CategoryId))
            .ToListAsync();
        return Ok(featured);
    }
}
