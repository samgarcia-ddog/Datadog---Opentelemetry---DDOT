namespace GorraShop.API.Models;

public class Category
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";
    public string Slug     { get; set; } = "";

    public ICollection<Product> Products { get; set; } = [];
}

public class Product
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = "";
    public string   Description { get; set; } = "";
    public decimal  Price       { get; set; }
    public int      Stock       { get; set; }
    public string   ImageUrl    { get; set; } = "";
    public string   Brand       { get; set; } = "";
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public int      CategoryId  { get; set; }
    public Category Category    { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

public class Order
{
    public int         Id            { get; set; }
    public string      CustomerEmail { get; set; } = "";
    public string      CustomerName  { get; set; } = "";
    public string      Address       { get; set; } = "";
    public OrderStatus Status        { get; set; } = OrderStatus.Pending;
    public decimal     Total         { get; set; }
    public DateTime    CreatedAt     { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int     Id        { get; set; }
    public int     OrderId   { get; set; }
    public Order   Order     { get; set; } = null!;
    public int     ProductId { get; set; }
    public Product Product   { get; set; } = null!;
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

// DTOs
public record ProductDto(
    int     Id,
    string  Name,
    string  Description,
    decimal Price,
    int     Stock,
    string  ImageUrl,
    string  Brand,
    string  CategoryName,
    int     CategoryId
);

public record CartItemDto(
    string  ProductId,
    string  ProductName,
    decimal UnitPrice,
    int     Quantity,
    string  ImageUrl
);

public record CreateOrderRequest(
    string CustomerEmail,
    string CustomerName,
    string Address,
    List<OrderLineRequest> Items
);

public record OrderLineRequest(int ProductId, int Quantity);

public record CheckoutRequest(
    string SessionId,
    string CustomerEmail,
    string CustomerName,
    string Address
);
