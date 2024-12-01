using DataEntities;
using Microsoft.EntityFrameworkCore;

namespace Products.Data;

public class ProductDataContext : DbContext
{
    public ProductDataContext(DbContextOptions<ProductDataContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Product { get; set; } = default!;
}

public static class Extensions
{
    public static void CreateDbIfNotExists(this IHost host)
    {
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProductDataContext>();
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context);
    }
}


public static class DbInitializer
{
    public static void Initialize(ProductDataContext context)
    {
        if (context.Product.Any())
            return;

        #region #region product list V1
        var products = new List<Product>
        {
            new Product { Name = "Solar Powered Flashlight", Description = "A fantastic product for outdoor enthusiasts", Price = 19.99m, ImageUrl = "product1.png" },
            new Product { Name = "Hiking Poles", Description = "Ideal for camping and hiking trips", Price = 24.99m, ImageUrl = "product2.png" },
            new Product { Name = "Outdoor Rain Jacket", Description = "This product will keep you warm and dry in all weathers", Price = 49.99m, ImageUrl = "product3.png" },
            new Product { Name = "Survival Kit", Description = "A must-have emergency item for any outdoor adventurer", Price = 99.99m, ImageUrl = "product4.png" },
            new Product { Name = "Outdoor Backpack", Description = "This backpack is perfect for carrying all your outdoor essentials", Price = 39.99m, ImageUrl = "product5.png" },
            new Product { Name = "Camping Cookware", Description = "This cookware set is ideal for cooking outdoors", Price = 29.99m, ImageUrl = "product6.png" },
            new Product { Name = "Camping Stove", Description = "This stove is perfect for cooking outdoors", Price = 49.99m, ImageUrl = "product7.png" },
            new Product { Name = "Camping Lantern", Description = "This lantern is perfect for lighting up your campsite", Price = 19.99m, ImageUrl = "product8.png" },
            new Product { Name = "Camping Tent", Description = "This tent is perfect for camping trips", Price = 99.99m, ImageUrl = "product9.png" },
        };

        #endregion

        #region product list V2
        ////var products = new List<Product>
        ////{
        ////    new Product
        ////    {
        ////        Name = "HoloSneaks 3000",
        ////        Description = "Holographic sneakers that glow with rainbow lights at every step. 'These aren’t just shoes—they’re a fashion revolution!'",
        ////        Price = 299.99m,
        ////        ImageUrl = "HoloSneaks_3000.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "TimeTraveler’s Jacket",
        ////        Description = "A jacket from the future. Wear it, and you’ll never be late again (except for after-work drinks).",
        ////        Price = 499.99m,
        ////        ImageUrl = "TimeTravelers_Jacket.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Solar-Powered Cap",
        ////        Description = "A solar-charging cap. No more worrying about phone batteries under the scorching sun. 'Brains and style in one cap!'",
        ////        Price = 89.99m,
        ////        ImageUrl = "Solar_Powered_Cap.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "AI Assistant Socks",
        ////        Description = "Socks with AI. They warn you about foot odor and track your activity. Warning: they might nag, 'Don’t turn me inside out!'",
        ////        Price = 39.99m,
        ////        ImageUrl = "AI_Assistant_Socks.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "HoverBelt",
        ////        Description = "A belt that lets you hover slightly above the ground. Perfect for weighing less on the scale!",
        ////        Price = 199.99m,
        ////        ImageUrl = "HoverBelt.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Quantum Scarf",
        ////        Description = "Adjusts to the perfect temperature. Warm in winter, cool in summer. 'Wrap yourself in quantum comfort!'",
        ////        Price = 99.99m,
        ////        ImageUrl = "Quantum_Scarf.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Fashion Guru Glasses",
        ////        Description = "Glasses that critique your outfit in real time. Your personal AI stylist on your face.",
        ////        Price = 249.99m,
        ////        ImageUrl = "Fashion_Guru_Glasses.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Cloud Hat",
        ////        Description = "A hat that feels like a real cloud. Acts as an umbrella in rain and provides shade in the sun.",
        ////        Price = 129.99m,
        ////        ImageUrl = "Cloud_Hat.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "NeonShoes X",
        ////        Description = "LED shoes that light up the dance floor. Guaranteed to make you the center of attention. Warning: excessive brightness may result in fines.",
        ////        Price = 149.99m,
        ////        ImageUrl = "NeonShoes_X.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Infinity T-Shirt",
        ////        Description = "A T-shirt that cleans itself. Wear it forever without washing! (Disclaimer: it doesn’t clean your life problems.)",
        ////        Price = 79.99m,
        ////        ImageUrl = "Infinity_T_Shirt.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Smart Pocket Pants",
        ////        Description = "Pockets appear only when needed. Warning: losing your card is still your responsibility.",
        ////        Price = 139.99m,
        ////        ImageUrl = "Smart_Pocket_Pants.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Gamer’s Hoodie",
        ////        Description = "Built-in headphones and hand warmers! 'The ultimate hoodie for gamers!'",
        ////        Price = 89.99m,
        ////        ImageUrl = "Gamers_Hoodie.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Hologram Earrings",
        ////        Description = "Earrings that project holograms of butterflies, stars, or even cats. 'Start conversations with your ears!'",
        ////        Price = 49.99m,
        ////        ImageUrl = "Hologram_Earrings.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Teleport Shoes",
        ////        Description = "Shoes that teleport you to work in 5 seconds. Only works if your workplace doesn’t have traffic jams.",
        ////        Price = 999.99m,
        ////        ImageUrl = "Teleport_Shoes.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Anti-Social Mask",
        ////        Description = "Hate conversations? The mask displays 'Battery Low' to keep people away.",
        ////        Price = 59.99m,
        ////        ImageUrl = "Anti_Social_Mask.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Mood Ring Glasses",
        ////        Description = "Glasses that change color based on your mood. Finally, a way to visually show 'I’m mad.'",
        ////        Price = 69.99m,
        ////        ImageUrl = "Mood_Ring_Glasses.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Digital Cape",
        ////        Description = "A cape with an LED screen. Display messages, ads, or even stock prices in real time.",
        ////        Price = 299.99m,
        ////        ImageUrl = "Digital_Cape.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Infinity Socks",
        ////        Description = "Socks that repair themselves when torn. 'Immortal socks for an immortal you.'",
        ////        Price = 19.99m,
        ////        ImageUrl = "Infinity_Socks.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Rechargeable Beanie",
        ////        Description = "A beanie with a built-in battery to keep your ears warm. USB-C charging supported!",
        ////        Price = 39.99m,
        ////        ImageUrl = "Rechargeable_Beanie.png"
        ////    },
        ////    new Product
        ////    {
        ////        Name = "Wi-Fi Scarf",
        ////        Description = "A scarf that doubles as a Wi-Fi hotspot. 'Your scarf, your connection to the world.'",
        ////        Price = 159.99m,
        ////        ImageUrl = "Wi_Fi_Scarf.png"
        ////    }
        ////}; 
        #endregion

        context.AddRange(products);

        context.SaveChanges();
    }
}