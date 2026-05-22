using GorraShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GorraShop.API.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Name = "Snapback",    Slug = "snapback" },
            new() { Name = "Fitted",      Slug = "fitted" },
            new() { Name = "Trucker",     Slug = "trucker" },
            new() { Name = "Bucket Hat",  Slug = "bucket-hat" },
            new() { Name = "Beanie",      Slug = "beanie" },
        };

        await db.Categories.AddRangeAsync(categories);
        await db.SaveChangesAsync();

        var snapback   = categories[0];
        var fitted     = categories[1];
        var trucker    = categories[2];
        var bucketHat  = categories[3];
        var beanie     = categories[4];

        var products = new List<Product>
        {
            // Snapbacks
            new() { Name = "Classic NY Snapback",       Brand = "47 Brand",    Price = 35.99m,  Stock = 50, CategoryId = snapback.Id,  ImageUrl = "https://placehold.co/400x400/111827/ffffff?text=NY+Snapback",  Description = "Snapback clásico bordado del equipo de Nueva York." },
            new() { Name = "Supreme Box Logo Snap",     Brand = "Supreme",     Price = 89.00m,  Stock = 15, CategoryId = snapback.Id,  ImageUrl = "https://placehold.co/400x400/CC0000/ffffff?text=Supreme",      Description = "Snapback con el icónico box logo de Supreme." },
            new() { Name = "Chicago Bulls Retro",       Brand = "Mitchell&N",  Price = 45.00m,  Stock = 30, CategoryId = snapback.Id,  ImageUrl = "https://placehold.co/400x400/CE1141/ffffff?text=Bulls",        Description = "Edición retro del equipo de Chicago." },
            new() { Name = "Strapback 3D Logo",         Brand = "New Era",     Price = 32.00m,  Stock = 40, CategoryId = snapback.Id,  ImageUrl = "https://placehold.co/400x400/1a1a2e/ffffff?text=3D+Logo",      Description = "Logo 3D bordado, correa ajustable trasera." },

            // Fitted
            new() { Name = "59FIFTY LA Dodgers",        Brand = "New Era",     Price = 42.99m,  Stock = 60, CategoryId = fitted.Id,    ImageUrl = "https://placehold.co/400x400/003DA5/ffffff?text=LA+Fitted",    Description = "El icónico 59FIFTY de los Dodgers de Los Ángeles." },
            new() { Name = "59FIFTY Yankees Pinstripe", Brand = "New Era",     Price = 44.99m,  Stock = 45, CategoryId = fitted.Id,    ImageUrl = "https://placehold.co/400x400/1c2841/ffffff?text=Yankees",      Description = "Fitted a rayas con el logo bordado de los Yankees." },
            new() { Name = "MLB All-Star Game 2024",    Brand = "New Era",     Price = 55.00m,  Stock = 20, CategoryId = fitted.Id,    ImageUrl = "https://placehold.co/400x400/0f3460/ffffff?text=All-Star",     Description = "Edición limitada del All-Star Game 2024." },

            // Trucker
            new() { Name = "Foam Trucker Mesh",         Brand = "Yupoong",     Price = 22.00m,  Stock = 80, CategoryId = trucker.Id,   ImageUrl = "https://placehold.co/400x400/2d6a4f/ffffff?text=Trucker",      Description = "Trucker clásico con panel de malla transpirable." },
            new() { Name = "Richardson 112 Solid",      Brand = "Richardson",  Price = 26.00m,  Stock = 65, CategoryId = trucker.Id,   ImageUrl = "https://placehold.co/400x400/457b9d/ffffff?text=R112",         Description = "El trucker más popular para personalización." },
            new() { Name = "Vintage Wash Trucker",      Brand = "Carhartt",    Price = 34.00m,  Stock = 35, CategoryId = trucker.Id,   ImageUrl = "https://placehold.co/400x400/8b7355/ffffff?text=Carhartt",     Description = "Lavado vintage, parche frontal de tela." },
            new() { Name = "Camo Trucker Cap",          Brand = "Realtree",    Price = 28.00m,  Stock = 40, CategoryId = trucker.Id,   ImageUrl = "https://placehold.co/400x400/556b2f/ffffff?text=Camo",         Description = "Estampado de camuflaje Realtree EDGE." },

            // Bucket Hats
            new() { Name = "Reversible Bucket Hat",     Brand = "Nike",        Price = 38.00m,  Stock = 55, CategoryId = bucketHat.Id, ImageUrl = "https://placehold.co/400x400/e63946/ffffff?text=Reversible",   Description = "Reversible: dos looks en uno." },
            new() { Name = "Bucket Hat Tie-Dye",        Brand = "Adidas",      Price = 35.00m,  Stock = 30, CategoryId = bucketHat.Id, ImageUrl = "https://placehold.co/400x400/7209b7/ffffff?text=Tie-Dye",      Description = "Tie-dye artesanal, algodón 100%." },
            new() { Name = "Outdoor Bucket UPF 50+",    Brand = "Columbia",    Price = 42.00m,  Stock = 45, CategoryId = bucketHat.Id, ImageUrl = "https://placehold.co/400x400/2c7873/ffffff?text=UPF50",        Description = "Protección solar UPF 50+ para actividades al aire libre." },
            new() { Name = "Monogram Bucket Hat",       Brand = "Gucci Rep",   Price = 65.00m,  Stock = 12, CategoryId = bucketHat.Id, ImageUrl = "https://placehold.co/400x400/c9a227/000000?text=Monogram",    Description = "Estampado de monograma lujo, edición limitada." },

            // Beanies
            new() { Name = "Pom-Pom Knit Beanie",      Brand = "Carhartt",    Price = 28.00m,  Stock = 90, CategoryId = beanie.Id,    ImageUrl = "https://placehold.co/400x400/2b2d42/ffffff?text=Pom-Pom",     Description = "Beanie de punto con pompón y forro polar interior." },
            new() { Name = "Cuffed Logo Beanie",        Brand = "The North F", Price = 32.00m,  Stock = 70, CategoryId = beanie.Id,    ImageUrl = "https://placehold.co/400x400/023e8a/ffffff?text=TNF",          Description = "Beanie doblado con logo bordado del lado." },
            new() { Name = "Slouchy Ribbed Beanie",     Brand = "Patagonia",   Price = 30.00m,  Stock = 55, CategoryId = beanie.Id,    ImageUrl = "https://placehold.co/400x400/588157/ffffff?text=Slouchy",      Description = "Tejido de punto acanalado, estilo relajado." },
            new() { Name = "Skull Cap Beanie",          Brand = "Nike",        Price = 22.00m,  Stock = 85, CategoryId = beanie.Id,    ImageUrl = "https://placehold.co/400x400/d62828/ffffff?text=Skull",        Description = "Skull cap ajustado para deportes de invierno." },
            new() { Name = "Merino Wool Beanie",        Brand = "Smartwool",   Price = 48.00m,  Stock = 25, CategoryId = beanie.Id,    ImageUrl = "https://placehold.co/400x400/3a0ca3/ffffff?text=Merino",       Description = "Lana merino 100%, suave, cálida y anti-olor." },
        };

        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();
    }
}
