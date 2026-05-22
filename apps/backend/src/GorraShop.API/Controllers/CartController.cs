using System.Text.Json;
using GorraShop.API.Data;
using GorraShop.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GorraShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(
    IDistributedCache cache,
    AppDbContext db,
    ILogger<CartController> logger) : ControllerBase
{
    // Cada sesión de carrito se identifica con un sessionId (generado por el frontend)
    private static string CacheKey(string sessionId) => $"cart:{sessionId}";

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetCart(string sessionId)
    {
        var items = await GetCartItems(sessionId);
        var total = items.Sum(i => i.UnitPrice * i.Quantity);
        return Ok(new { SessionId = sessionId, Items = items, Total = total, Count = items.Sum(i => i.Quantity) });
    }

    [HttpPost("{sessionId}/add")]
    public async Task<IActionResult> AddItem(string sessionId, [FromBody] AddToCartRequest request)
    {
        var product = await db.Products.FindAsync(request.ProductId);
        if (product is null) return NotFound(new { Error = "Producto no encontrado" });
        if (product.Stock < request.Quantity) return BadRequest(new { Error = "Stock insuficiente" });

        var items = await GetCartItems(sessionId);

        var existing = items.FirstOrDefault(i => i.ProductId == request.ProductId.ToString());
        if (existing is not null)
        {
            items = items.Select(i => i.ProductId == request.ProductId.ToString()
                ? i with { Quantity = i.Quantity + request.Quantity }
                : i).ToList();
        }
        else
        {
            items.Add(new CartItemDto(
                ProductId:   product.Id.ToString(),
                ProductName: product.Name,
                UnitPrice:   product.Price,
                Quantity:    request.Quantity,
                ImageUrl:    product.ImageUrl
            ));
        }

        await SaveCartItems(sessionId, items);
        logger.LogInformation("Added {Qty}x {Product} to cart {Session}", request.Quantity, product.Name, sessionId);

        return Ok(new { Message = "Agregado al carrito", ItemCount = items.Sum(i => i.Quantity) });
    }

    [HttpDelete("{sessionId}/remove/{productId}")]
    public async Task<IActionResult> RemoveItem(string sessionId, int productId)
    {
        var items = await GetCartItems(sessionId);
        items = items.Where(i => i.ProductId != productId.ToString()).ToList();
        await SaveCartItems(sessionId, items);
        return Ok(new { Message = "Eliminado del carrito" });
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> ClearCart(string sessionId)
    {
        await cache.RemoveAsync(CacheKey(sessionId));
        return Ok(new { Message = "Carrito limpiado" });
    }

    private async Task<List<CartItemDto>> GetCartItems(string sessionId)
    {
        var json = await cache.GetStringAsync(CacheKey(sessionId));
        return json is null
            ? []
            : JsonSerializer.Deserialize<List<CartItemDto>>(json) ?? [];
    }

    private async Task SaveCartItems(string sessionId, List<CartItemDto> items)
    {
        var json = JsonSerializer.Serialize(items);
        await cache.SetStringAsync(CacheKey(sessionId), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    }
}

public record AddToCartRequest(int ProductId, int Quantity = 1);
