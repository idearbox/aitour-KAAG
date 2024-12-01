using SearchEntities;
using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using System.Diagnostics;
using Products.Memory;

namespace Products.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");

        //01..GetAllProducts
        group.MapGet("/", async (ProductDataContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        //02..GetProductById
        group.MapGet("/{id}", async (int id, ProductDataContext db) =>
        {
            return await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Product model
                    ? Results.Ok(model)
                    : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        //03..UpdateProduct
        group.MapPut("/{id}", async (int id, Product product, ProductDataContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.Id, product.Id)
                  .SetProperty(m => m.Name, product.Name)
                  .SetProperty(m => m.Description, product.Description)
                  .SetProperty(m => m.Price, product.Price)
                  .SetProperty(m => m.ImageUrl, product.ImageUrl)
                );

            return affected == 1 ? Results.Ok() : Results.NotFound();
        })
        .WithName("UpdateProduct")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status204NoContent);

        //04..CreateProduct
        group.MapPost("/", async (Product product, ProductDataContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/Product/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .Produces<Product>(StatusCodes.Status201Created);

        //05..DeleteProduct
        group.MapDelete("/{id}", async (int id, ProductDataContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1 ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        #region 06..DB 검색
        group.MapGet("/searchDB/{search}", async (string search, ProductDataContext db) =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            List<Product> products = await db.Product
            .Where(p => EF.Functions.Like(p.Name, $"%{search}%"))
            .ToListAsync();

            stopwatch.Stop();

            var response = new SearchResponse();
            response.Products = products;
            response.Response = products.Count > 0 ?
                $"{products.Count} Products found for [{search}]" :
                $"No products found for [{search}]";
            response.ElapsedTime = stopwatch.Elapsed;
            return response;
        })
            .WithName("SearchAllProductsByDB")
            .Produces<List<Product>>(StatusCodes.Status200OK);
        #endregion

        #region 07..AI 검색 Endpoint
        group.MapGet("/searchAI/{search}",
            async (string search, ProductDataContext db, AIMemoryContext mc) =>
            {
                var result = await mc.Search(search, db);
                return Results.Ok(result);
            })
            .WithName("SearchAllProductsByAI")
            .Produces<SearchResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        #endregion
    }
}
