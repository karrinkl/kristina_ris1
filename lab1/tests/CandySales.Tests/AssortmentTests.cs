using System.Linq;
using CandySales.Domain;
using Xunit;

namespace CandySales.Tests;

public class AssortmentTests
{
    [Fact]
    public void AddCandy_AssignsSequentialIds()
    {
        var factory = TestFactory.CreateEmpty();

        var first = factory.AddCandy("Ириска", CandyCategory.Toffee, 520m, 10m);
        var second = factory.AddCandy("Драже", CandyCategory.Dragee, 690m, 20m);

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
        Assert.Equal(2, factory.CandyCount);
        Assert.Same(second, factory.FindCandy(2));
    }

    [Fact]
    public void AddCandy_WithDuplicateNameIgnoringCase_Throws()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.AddCandy("ЛЕСНАЯ СКАЗКА", CandyCategory.Chocolate, 900m, 1m));
        Assert.Equal(4, factory.CandyCount);
    }

    [Fact]
    public void AddCandy_WithInvalidData_DoesNotChangeAssortment()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.AddCandy("Новые", CandyCategory.Jelly, 0m, 10m));
        Assert.Equal(4, factory.CandyCount);
    }

    [Fact]
    public void FindCandy_WithUnknownId_ReturnsNull()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Null(factory.FindCandy(999));
        Assert.Throws<BusinessRuleException>(() => factory.GetCandy(999));
    }

    [Fact]
    public void SearchByName_FindsByFragmentIgnoringCase()
    {
        var factory = TestFactory.CreateWithAssortment();

        var found = factory.SearchByName("  КАРАМЕЛЬ ").Select(candy => candy.Name).ToArray();

        Assert.Equal(new[] { "Барбарисовая карамель", "Мятная карамель" }, found);
    }

    [Fact]
    public void SearchByName_WithoutMatches_ReturnsEmpty()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Empty(factory.SearchByName("шоколадный заяц"));
    }

    [Fact]
    public void FilterCandies_ByCategory_ReturnsOnlyThatCategory()
    {
        var factory = TestFactory.CreateWithAssortment();

        var caramel = factory.FilterCandies(candy => candy.Category == CandyCategory.Caramel);

        Assert.Equal(2, caramel.Count);
        Assert.All(caramel, candy => Assert.Equal(CandyCategory.Caramel, candy.Category));
    }

    [Fact]
    public void FilterCandies_ByPriceRangeAndLowStock_UsesPredicate()
    {
        var factory = TestFactory.CreateWithAssortment();

        var midPrice = factory.FilterCandies(candy => candy.PricePerKg >= 400m && candy.PricePerKg <= 500m);
        var lowStock = factory.FilterCandies(candy => candy.StockKg < ConfectioneryFactory.LowStockThresholdKg);

        Assert.Equal(new[] { 2, 4 }, midPrice.Select(candy => candy.Id).ToArray());
        Assert.Equal("Мятная карамель", Assert.Single(lowStock).Name);
    }

    [Theory]
    [InlineData(CandySortField.Price, false, new[] { 3, 2, 4, 1 })]
    [InlineData(CandySortField.Price, true, new[] { 1, 4, 2, 3 })]
    [InlineData(CandySortField.Stock, false, new[] { 3, 4, 1, 2 })]
    [InlineData(CandySortField.Stock, true, new[] { 2, 1, 4, 3 })]
    public void SortCandies_ByNumericField_OrdersCorrectly(CandySortField field, bool descending, int[] expectedIds)
    {
        var factory = TestFactory.CreateWithAssortment();

        var ids = factory.SortCandies(field, descending).Select(candy => candy.Id).ToArray();

        Assert.Equal(expectedIds, ids);
    }

    [Fact]
    public void SortCandies_ByName_OrdersAlphabetically()
    {
        var factory = TestFactory.CreateWithAssortment();

        var names = factory.SortCandies(CandySortField.Name).Select(candy => candy.Name).ToArray();

        Assert.Equal(new[] { "Барбарисовая карамель", "Лесная сказка", "Мятная карамель", "Помадка крем-брюле" }, names);
    }

    [Fact]
    public void UpdateCandy_ChangesOnlySpecifiedValues()
    {
        var factory = TestFactory.CreateWithAssortment();

        factory.UpdateCandy(1, newPricePerKg: 900m);
        var candy = factory.GetCandy(1);

        Assert.Equal(900m, candy.PricePerKg);
        Assert.Equal("Лесная сказка", candy.Name);
        Assert.Equal(CandyCategory.Chocolate, candy.Category);
    }

    [Fact]
    public void UpdateCandy_WithInvalidPrice_DoesNotApplyOtherChanges()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.UpdateCandy(1, newName: "Новое имя", newPricePerKg: -1m));
        Assert.Equal("Лесная сказка", factory.GetCandy(1).Name);
    }

    [Fact]
    public void UpdateCandy_ToNameOfAnotherCandy_Throws()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.UpdateCandy(1, newName: "Мятная карамель"));
    }

    [Fact]
    public void AddProducedBatch_IncreasesStock()
    {
        var factory = TestFactory.CreateWithAssortment();

        factory.AddProducedBatch(3, 30.5m);

        Assert.Equal(45.5m, factory.GetCandy(3).StockKg);
    }

    [Fact]
    public void RemoveCandy_WithoutSales_RemovesIt()
    {
        var factory = TestFactory.CreateWithAssortment();

        factory.RemoveCandy(4);

        Assert.Null(factory.FindCandy(4));
        Assert.Equal(3, factory.CandyCount);
    }

    [Fact]
    public void RemoveCandy_WithSales_ThrowsAndKeepsCandy()
    {
        var factory = TestFactory.CreateWithAssortment();
        factory.SellCandy(1, 1m);

        Assert.Throws<BusinessRuleException>(() => factory.RemoveCandy(1));
        Assert.NotNull(factory.FindCandy(1));
    }
}
