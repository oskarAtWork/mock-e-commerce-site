using MockEcommerce.Api.Models;
using MockEcommerce.Api.Services;

namespace MockEcommerce.Api.Tests.Services;

public class InMemoryCartServiceTests
{
    private readonly InMemoryCartService _sut = new();

    [Fact]
    public void GetAll_Initially_ReturnsEmptyList()
    {
        var result = _sut.GetAll();

        Assert.Empty(result);
    }

    [Fact]
    public void Add_NewItem_InsertsAndReturnsItem()
    {
        var item = new CartItem { ProductId = 1, ProductName = "Headphones", UnitPrice = 79.99m, Quantity = 1 };

        var result = _sut.Add(item);

        Assert.Equal(1, result.ProductId);
        Assert.Equal(1, result.Quantity);
        Assert.Single(_sut.GetAll());
    }

    [Fact]
    public void Add_ExistingItem_IncrementsQuantityAndReturnsItem()
    {
        _sut.Add(new CartItem { ProductId = 1, ProductName = "Headphones", UnitPrice = 79.99m, Quantity = 2 });

        var result = _sut.Add(new CartItem { ProductId = 1, ProductName = "Headphones", UnitPrice = 79.99m, Quantity = 1 });

        Assert.Equal(3, result.Quantity);
        Assert.Single(_sut.GetAll());
    }

    [Fact]
    public void GetByProductId_ExistingItem_ReturnsItem()
    {
        _sut.Add(new CartItem { ProductId = 2, ProductName = "Shoes", UnitPrice = 59.99m, Quantity = 1 });

        var result = _sut.GetByProductId(2);

        Assert.NotNull(result);
        Assert.Equal(2, result.ProductId);
    }

    [Fact]
    public void GetByProductId_NonExistentItem_ReturnsNull()
    {
        var result = _sut.GetByProductId(999);

        Assert.Null(result);
    }

    [Fact]
    public void Remove_ExistingItem_ReturnsTrueAndRemovesItem()
    {
        _sut.Add(new CartItem { ProductId = 1, ProductName = "Headphones", UnitPrice = 79.99m, Quantity = 1 });

        var result = _sut.Remove(1);

        Assert.True(result);
        Assert.Empty(_sut.GetAll());
    }

    [Fact]
    public void Remove_NonExistentItem_ReturnsFalse()
    {
        var result = _sut.Remove(999);

        Assert.False(result);
    }

    [Fact]
    public void Clear_WithItems_EmptiesCart()
    {
        _sut.Add(new CartItem { ProductId = 1, ProductName = "Headphones", UnitPrice = 79.99m, Quantity = 1 });
        _sut.Add(new CartItem { ProductId = 2, ProductName = "Shoes", UnitPrice = 59.99m, Quantity = 2 });

        _sut.Clear();

        Assert.Empty(_sut.GetAll());
    }

    [Fact]
    public void Update_ExistingItem_SetsQuantityAndReturnsItem()
    {
        _sut.Add(new CartItem { ProductId = 1, ProductName = "Headphones", UnitPrice = 79.99m, Quantity = 1 });

        var result = _sut.Update(1, 4);

        Assert.NotNull(result);
        Assert.Equal(4, result.Quantity);
    }

    [Fact]
    public void Update_NonExistentItem_ReturnsNull()
    {
        var result = _sut.Update(999, 3);

        Assert.Null(result);
    }
}
