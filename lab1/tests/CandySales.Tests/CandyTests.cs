using CandySales.Domain;
using Xunit;

namespace CandySales.Tests;

public class CandyTests
{
    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var candy = new Candy
        {
            Id = 1,
            Name = "  Лесная сказка ",
            Category = CandyCategory.Chocolate,
            PricePerKg = 845.50m,
            StockKg = 12.345m,
        };

        Assert.Equal(1, candy.Id);
        Assert.Equal("Лесная сказка", candy.Name);
        Assert.Equal(CandyCategory.Chocolate, candy.Category);
        Assert.Equal(845.50m, candy.PricePerKg);
        Assert.Equal(12.345m, candy.StockKg);
        Assert.True(candy.IsInStock);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        Assert.Throws<BusinessRuleException>(() => new Candy
        {
            Id = 1,
            Name = name,
            Category = CandyCategory.Caramel,
            PricePerKg = 100m,
        });
    }

    [Fact]
    public void Create_WithTooLongName_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => new Candy
        {
            Id = 1,
            Name = new string('к', 61),
            Category = CandyCategory.Caramel,
            PricePerKg = 100m,
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveId_Throws(int id)
    {
        Assert.Throws<BusinessRuleException>(() => new Candy
        {
            Id = id,
            Name = "Карамель",
            Category = CandyCategory.Caramel,
            PricePerKg = 100m,
        });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("10.555")]
    [InlineData("100000.01")]
    public void Create_WithInvalidPrice_Throws(string price)
    {
        Assert.Throws<BusinessRuleException>(() => new Candy
        {
            Id = 1,
            Name = "Карамель",
            Category = CandyCategory.Caramel,
            PricePerKg = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture),
        });
    }

    [Fact]
    public void Create_WithUndefinedCategory_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => new Candy
        {
            Id = 1,
            Name = "Карамель",
            Category = (CandyCategory)100,
            PricePerKg = 100m,
        });
    }

    [Fact]
    public void Create_WithNegativeStock_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => new Candy
        {
            Id = 1,
            Name = "Карамель",
            Category = CandyCategory.Caramel,
            PricePerKg = 100m,
            StockKg = -0.001m,
        });
    }

    [Fact]
    public void Create_WithoutStock_HasZeroStockAndIsNotInStock()
    {
        var candy = new Candy { Id = 1, Name = "Суфле", Category = CandyCategory.Souffle, PricePerKg = 760m };

        Assert.Equal(0m, candy.StockKg);
        Assert.False(candy.IsInStock);
    }

    [Fact]
    public void AddStock_IncreasesStock()
    {
        var candy = new Candy { Id = 1, Name = "Суфле", Category = CandyCategory.Souffle, PricePerKg = 760m, StockKg = 1.5m };

        candy.AddStock(10.25m);

        Assert.Equal(11.75m, candy.StockKg);
    }

    [Fact]
    public void AddStock_WithZeroQuantity_Throws()
    {
        var candy = new Candy { Id = 1, Name = "Суфле", Category = CandyCategory.Souffle, PricePerKg = 760m };

        Assert.Throws<BusinessRuleException>(() => candy.AddStock(0m));
    }

    [Fact]
    public void ChangingPrice_ToInvalidValue_KeepsPreviousPrice()
    {
        var candy = new Candy { Id = 1, Name = "Суфле", Category = CandyCategory.Souffle, PricePerKg = 760m };

        Assert.Throws<BusinessRuleException>(() => candy.PricePerKg = -5m);
        Assert.Equal(760m, candy.PricePerKg);
    }
}
