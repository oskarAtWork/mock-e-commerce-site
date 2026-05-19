using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MockEcommerce.Api.Models;

namespace MockEcommerce.Api.Tests.Endpoints;

public class CartEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CartEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task ClearCartAsync() => await _client.DeleteAsync("/api/cart");

    // GET /api/cart

    [Fact]
    public async Task GetCart_Initially_ReturnsOkWithEmptyArray()
    {
        await ClearCartAsync();

        var response = await _client.GetAsync("/api/cart");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<CartItem>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    // POST /api/cart

    [Fact]
    public async Task PostCart_ValidProduct_Returns201WithCartItem()
    {
        await ClearCartAsync();

        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<CartItem>();
        Assert.NotNull(item);
        Assert.Equal(1, item.ProductId);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public async Task PostCart_SameProductAgain_Returns200WithIncrementedQuantity()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<CartItem>();
        Assert.NotNull(item);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task PostCart_UnknownProduct_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 9999, quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostCart_QuantityZero_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCart_NegativeQuantity_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCart_QuantityFive_Returns201()
    {
        await ClearCartAsync();

        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<CartItem>();
        Assert.NotNull(item);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public async Task PostCart_QuantityExceedsMaxTotal_Returns400()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 4 });

        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCart_QuantityResultsInExactlyFiveTotal_Returns200()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 2 });

        var response = await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<CartItem>();
        Assert.NotNull(item);
        Assert.Equal(5, item.Quantity);
    }

    // PUT /api/cart/{productId}

    [Fact]
    public async Task PutCart_ValidData_Returns200WithUpdatedItem()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        var response = await _client.PutAsJsonAsync("/api/cart/1", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<CartItem>();
        Assert.NotNull(item);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task PutCart_QuantityFive_Returns200()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        var response = await _client.PutAsJsonAsync("/api/cart/1", new { quantity = 5 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<CartItem>();
        Assert.NotNull(item);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public async Task PutCart_QuantitySix_Returns400()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        var response = await _client.PutAsJsonAsync("/api/cart/1", new { quantity = 6 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCart_QuantityZero_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/cart/1", new { quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCart_NegativeQuantity_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/cart/1", new { quantity = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCart_ItemNotInCart_Returns404()
    {
        await ClearCartAsync();

        var response = await _client.PutAsJsonAsync("/api/cart/1", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutCart_UnknownProduct_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/cart/9999", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/cart/{productId}

    [Fact]
    public async Task DeleteCartItem_ExistingItem_Returns204AndItemRemoved()
    {
        await ClearCartAsync();
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });

        var response = await _client.DeleteAsync("/api/cart/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cartResponse = await _client.GetAsync("/api/cart");
        var items = await cartResponse.Content.ReadFromJsonAsync<List<CartItem>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task DeleteCartItem_UnknownItem_Returns404()
    {
        await ClearCartAsync();

        var response = await _client.DeleteAsync("/api/cart/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/cart

    [Fact]
    public async Task DeleteCart_Returns204AndCartIsEmpty()
    {
        await _client.PostAsJsonAsync("/api/cart", new { productId = 1, quantity = 1 });
        await _client.PostAsJsonAsync("/api/cart", new { productId = 2, quantity = 1 });

        var response = await _client.DeleteAsync("/api/cart");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cartResponse = await _client.GetAsync("/api/cart");
        var items = await cartResponse.Content.ReadFromJsonAsync<List<CartItem>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }
}
