using Microsoft.AspNetCore.Http.HttpResults;
using MockEcommerce.Api.Models;
using MockEcommerce.Api.Services;

namespace MockEcommerce.Api.Endpoints;

/// <summary>
/// Maps shopping cart endpoints under <c>/api/cart</c>.
/// </summary>
public static class CartEndpoints
{
    /// <summary>Registers cart-related routes on the given endpoint route builder.</summary>
    public static void MapCartEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/cart")
            .WithTags("Cart");

        group.MapGet("/", GetCart)
            .WithName("GetCart")
            .WithSummary("Returns all items currently in the cart.");

        group.MapPost("/", AddToCart)
            .WithName("AddToCart")
            .WithSummary("Adds a product to the cart or increments quantity if already present.");

        group.MapPut("/{productId:int}", UpdateCartItem)
            .WithName("UpdateCartItem")
            .WithSummary("Sets the absolute quantity of an item already in the cart.");

        group.MapDelete("/{productId:int}", RemoveFromCart)
            .WithName("RemoveFromCart")
            .WithSummary("Removes a single product from the cart by its product ID.");

        group.MapDelete("/", ClearCart)
            .WithName("ClearCart")
            .WithSummary("Removes all items from the cart.");
    }

    /// <summary>Returns all items currently in the cart.</summary>
    internal static Ok<IEnumerable<CartItem>> GetCart(ICartService cartService)
    {
        return TypedResults.Ok<IEnumerable<CartItem>>(cartService.GetAll());
    }

    /// <summary>Adds a product to the cart or increments quantity if already present.</summary>
    internal static Results<Created<CartItem>, Ok<CartItem>, NotFound<string>, ValidationProblem> AddToCart(
        AddToCartRequest request,
        IProductService productService,
        ICartService cartService)
    {
        if (request.Quantity < 1)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be at least 1."]
            });

        var product = productService.GetById(request.ProductId);
        if (product is null)
            return TypedResults.NotFound("Product not found");

        var existing = cartService.GetByProductId(request.ProductId);
        var newTotal = (existing?.Quantity ?? 0) + request.Quantity;
        if (newTotal > 5)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["quantity"] = ["Cannot exceed 5 of any single item."]
            });

        var isNew = existing is null;
        var cartItem = cartService.Add(new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = request.Quantity
        });

        if (isNew)
            return TypedResults.Created($"/api/cart/{cartItem.ProductId}", cartItem);
        return TypedResults.Ok(cartItem);
    }

    /// <summary>Sets the absolute quantity of an item already in the cart.</summary>
    internal static Results<Ok<CartItem>, NotFound, NotFound<string>, ValidationProblem> UpdateCartItem(
        int productId,
        UpdateCartItemRequest request,
        IProductService productService,
        ICartService cartService)
    {
        if (request.Quantity < 1)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be at least 1."]
            });

        if (request.Quantity > 5)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity cannot exceed 5."]
            });

        var product = productService.GetById(productId);
        if (product is null)
            return TypedResults.NotFound("Product not found");

        var updated = cartService.Update(productId, request.Quantity);
        if (updated is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(updated);
    }

    /// <summary>Removes a single product from the cart by its product ID.</summary>
    internal static Results<NoContent, NotFound> RemoveFromCart(int productId, ICartService cartService)
    {
        return cartService.Remove(productId)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    /// <summary>Removes all items from the cart.</summary>
    internal static NoContent ClearCart(ICartService cartService)
    {
        cartService.Clear();
        return TypedResults.NoContent();
    }
}

/// <summary>Request body for adding a product to the cart.</summary>
public record AddToCartRequest(int ProductId, int Quantity);
