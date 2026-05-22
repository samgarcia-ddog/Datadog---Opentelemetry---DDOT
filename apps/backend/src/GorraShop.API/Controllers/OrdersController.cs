using System.Text.Json;
using GorraShop.API.Data;
using GorraShop.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GorraShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    AppDbContext db,
    IDistributedCache cache,
    ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        // Recuperar carrito desde Redis
        var cartJson = await cache.GetStringAsync($"cart:{request.SessionId}");
        if (string.IsNullOrEmpty(cartJson))
            return BadRequest(new { Error = "Carrito vacío o expirado" });

        var cartItems = JsonSerializer.Deserialize<List<CartItemDto>>(cartJson) ?? [];
        if (cartItems.Count == 0)
            return BadRequest(new { Error = "Carrito vacío" });

        // Validar stock y obtener precios actuales de la DB
        var productIds = cartItems.Select(i => int.Parse(i.ProductId)).ToList();
        var products   = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        foreach (var item in cartItems)
        {
            var product = products.FirstOrDefault(p => p.Id.ToString() == item.ProductId);
            if (product is null) return BadRequest(new { Error = $"Producto {item.ProductId} no encontrado" });
            if (product.Stock < item.Quantity)
                return BadRequest(new { Error = $"Stock insuficiente para {product.Name}" });
        }

        // Crear la orden
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            CustomerName  = request.CustomerName,
            Address       = request.Address,
            Status        = OrderStatus.Confirmed,
            Items         = cartItems.Select(item =>
            {
                var product = products.First(p => p.Id.ToString() == item.ProductId);
                return new OrderItem
                {
                    ProductId = product.Id,
                    Quantity  = item.Quantity,
                    UnitPrice = product.Price,
                };
            }).ToList()
        };

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        // Descontar stock
        foreach (var item in order.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.Stock -= item.Quantity;
        }

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        // Limpiar carrito
        await cache.RemoveAsync($"cart:{request.SessionId}");

        logger.LogInformation("Order {OrderId} created for {Email}. Total: {Total}",
            order.Id, order.CustomerEmail, order.Total);

        return Ok(new
        {
            OrderId      = order.Id,
            Status       = order.Status.ToString(),
            Total        = order.Total,
            ItemCount    = order.Items.Count,
            Message      = "Orden creada exitosamente"
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.CustomerEmail,
            order.CustomerName,
            order.Address,
            Status    = order.Status.ToString(),
            order.Total,
            order.CreatedAt,
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product.Name,
                i.Quantity,
                i.UnitPrice,
                Subtotal = i.Quantity * i.UnitPrice
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var total  = await db.Orders.CountAsync();
        var orders = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.CustomerEmail,
                o.CustomerName,
                Status    = o.Status.ToString(),
                o.Total,
                o.CreatedAt,
                ItemCount = o.Items.Count
            })
            .ToListAsync();

        return Ok(new { Total = total, Page = page, Items = orders });
    }
}
