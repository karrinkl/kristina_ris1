using System;
using System.Collections.Generic;
using System.Linq;
using CandySales.Domain;
using Xunit;

namespace CandySales.Tests;

public class SalesTests
{
    [Theory]
    [InlineData("2.5", "480.40", "1201.00")]
    [InlineData("0.333", "410.50", "136.70")] // 136.6965 -> 136.70
    [InlineData("0.001", "395.00", "0.40")] // 0.395 -> 0.40 (половина копейки округляется от нуля)
    [InlineData("12.5", "845.00", "10562.50")]
    public void CalculateTotal_MultipliesAndRoundsToKopecks(string quantity, string price, string expected)
    {
        var total = Sale.CalculateTotal(Parse(quantity), Parse(price));

        Assert.Equal(Parse(expected), total);
    }

    [Fact]
    public void SellCandy_CreatesSaleWithPriceAtMomentOfSale()
    {
        var factory = TestFactory.CreateWithAssortment();

        var sale = factory.SellCandy(4, 2.5m, "Магазин «Ласточка»");

        Assert.Equal(1, sale.Id);
        Assert.Equal(4, sale.CandyId);
        Assert.Equal("Помадка крем-брюле", sale.CandyName);
        Assert.Equal(2.5m, sale.QuantityKg);
        Assert.Equal(480.40m, sale.PricePerKg);
        Assert.Equal(1201.00m, sale.Total);
        Assert.Equal(TestFactory.Now, sale.SoldAt);
        Assert.Equal("Магазин «Ласточка»", sale.CustomerName);
        Assert.Equal(1, factory.SaleCount);
    }

    [Fact]
    public void SellCandy_DecreasesStock()
    {
        var factory = TestFactory.CreateWithAssortment();

        factory.SellCandy(1, 12.75m);

        Assert.Equal(87.25m, factory.GetCandy(1).StockKg);
    }

    [Fact]
    public void SellCandy_WholeStock_LeavesZero()
    {
        var factory = TestFactory.CreateWithAssortment();

        factory.SellCandy(3, 15m);

        Assert.Equal(0m, factory.GetCandy(3).StockKg);
        Assert.False(factory.GetCandy(3).IsInStock);
    }

    [Fact]
    public void SellCandy_WithInsufficientStock_ThrowsAndChangesNothing()
    {
        var factory = TestFactory.CreateWithAssortment();

        var error = Assert.Throws<BusinessRuleException>(() => factory.SellCandy(3, 15.001m));

        Assert.Contains("Недостаточно", error.Message);
        Assert.Equal(15m, factory.GetCandy(3).StockKg);
        Assert.Equal(0, factory.SaleCount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.5")]
    [InlineData("1.0005")]
    public void SellCandy_WithInvalidQuantity_Throws(string quantity)
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.SellCandy(1, Parse(quantity)));
        Assert.Equal(100m, factory.GetCandy(1).StockKg);
        Assert.Equal(0, factory.SaleCount);
    }

    [Fact]
    public void SellCandy_WithUnknownCandy_Throws()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.SellCandy(42, 1m));
    }

    [Fact]
    public void SellCandy_InFuture_Throws()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<BusinessRuleException>(() => factory.SellCandy(1, 1m, soldAt: TestFactory.Now.AddMinutes(1)));
    }

    [Fact]
    public void SellCandy_WithBlankCustomer_StoresNull()
    {
        var factory = TestFactory.CreateWithAssortment();

        var sale = factory.SellCandy(1, 1m, "   ");

        Assert.Null(sale.CustomerName);
    }

    [Fact]
    public void Sale_WithInvalidQuantity_CannotBeCreated()
    {
        Assert.Throws<BusinessRuleException>(() => new Sale
        {
            Id = 1,
            CandyId = 1,
            CandyName = "Тест",
            QuantityKg = 0m,
            PricePerKg = 100m,
            SoldAt = TestFactory.Now,
        });
    }

    [Fact]
    public void ChangingCandyPriceAndName_DoesNotChangeSalesHistory()
    {
        var factory = TestFactory.CreateWithAssortment();
        var sale = factory.SellCandy(1, 2m);

        factory.UpdateCandy(1, newName: "Лесная сказка (новая рецептура)", newPricePerKg: 999.99m);

        var stored = Assert.Single(factory.GetSales());
        Assert.Same(sale, stored);
        Assert.Equal(845.00m, stored.PricePerKg);
        Assert.Equal(1690.00m, stored.Total);
        Assert.Equal("Лесная сказка", stored.CandyName);
        Assert.Equal(1690.00m, factory.CalculateTotalRevenue());
    }

    [Fact]
    public void SellCandy_RaisesSaleCompletedEventOnce()
    {
        var factory = TestFactory.CreateWithAssortment();
        var received = new List<SaleCompletedEventArgs>();
        factory.SaleCompleted += (sender, e) =>
        {
            Assert.Same(factory, sender);
            received.Add(e);
        };

        var sale = factory.SellCandy(3, 5m);

        var args = Assert.Single(received);
        Assert.Same(sale, args.Sale);
        Assert.Equal(10m, args.RemainingStockKg);
        Assert.True(args.IsStockLow);
    }

    [Fact]
    public void FailedSale_DoesNotRaiseEvent()
    {
        var factory = TestFactory.CreateWithAssortment();
        var raised = false;
        factory.SaleCompleted += (_, _) => raised = true;

        Assert.Throws<BusinessRuleException>(() => factory.SellCandy(3, 100m));

        Assert.False(raised);
    }

    [Fact]
    public void CalculateRevenue_CountsOnlySalesInsidePeriod()
    {
        var factory = CreateWithSalesHistory();

        var march10To14 = new SalesPeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 14));
        var march15 = new SalesPeriod(new DateOnly(2025, 3, 15), new DateOnly(2025, 3, 15));

        Assert.Equal(4105.00m + 1690.00m + 1201.00m, factory.CalculateRevenue(march10To14));
        Assert.Equal(395.00m, factory.CalculateRevenue(march15));
        Assert.Equal(4105.00m + 1690.00m + 1201.00m + 395.00m, factory.CalculateTotalRevenue());
    }

    [Fact]
    public void CalculateRevenue_WithoutSales_IsZero()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Equal(0m, factory.CalculateTotalRevenue());
    }

    [Fact]
    public void FindSales_ByDateCandyNameAndCustomer()
    {
        var factory = CreateWithSalesHistory();

        Assert.Equal(new[] { 2, 3 }, factory.FindSalesByDate(new DateOnly(2025, 3, 14)).Select(s => s.Id).ToArray());
        Assert.Equal(new[] { 1, 4 }, factory.FindSalesByCandyName("карамель").Select(s => s.Id).ToArray());
        Assert.Equal(new[] { 1, 3 }, factory.FindSalesByCustomer("продторг").Select(s => s.Id).ToArray());
    }

    [Fact]
    public void GetSales_ReturnsChronologicalOrder()
    {
        var factory = TestFactory.CreateWithAssortment();
        factory.SellCandy(1, 1m, soldAt: new DateTime(2025, 3, 14, 10, 0, 0));
        factory.SellCandy(2, 1m, soldAt: new DateTime(2025, 3, 1, 10, 0, 0));

        Assert.Equal(new[] { 2, 1 }, factory.GetSales().Select(s => s.Id).ToArray());
    }

    [Fact]
    public void GetTopSellingCandies_OrdersByTotalWeight()
    {
        var factory = CreateWithSalesHistory();

        var top = factory.GetTopSellingCandies(2);

        Assert.Equal(2, top.Length);
        Assert.Equal(new CandySalesSummary(2, "Барбарисовая карамель", 10m, 4105.00m, 1), top[0]);
        Assert.Equal(4, top[1].CandyId);
        Assert.Equal(2.5m, top[1].TotalKg);
    }

    [Fact]
    public void GetTopSellingCandies_AggregatesSeveralSalesOfSameCandy()
    {
        var factory = TestFactory.CreateWithAssortment();
        factory.SellCandy(1, 1.5m);
        factory.SellCandy(1, 2.5m);

        var summary = Assert.Single(factory.GetTopSellingCandies(5));

        Assert.Equal(4m, summary.TotalKg);
        Assert.Equal(2, summary.SalesCount);
        Assert.Equal(3380.00m, summary.Revenue);
    }

    [Fact]
    public void GetTopSellingCandies_WithNonPositiveCount_Throws()
    {
        var factory = TestFactory.CreateWithAssortment();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.GetTopSellingCandies(0));
    }

    /// <summary>Четыре продажи за 10–15 марта 2025 года.</summary>
    private static ConfectioneryFactory CreateWithSalesHistory()
    {
        var factory = TestFactory.CreateWithAssortment();
        factory.SellCandy(2, 10m, "ООО «Продторг»", new DateTime(2025, 3, 10, 9, 0, 0)); // 4105.00
        factory.SellCandy(1, 2m, "Магазин «Ласточка»", new DateTime(2025, 3, 14, 11, 0, 0)); // 1690.00
        factory.SellCandy(4, 2.5m, "ООО «Продторг»", new DateTime(2025, 3, 14, 18, 30, 0)); // 1201.00
        factory.SellCandy(3, 1m, null, new DateTime(2025, 3, 15, 0, 0, 0)); // 395.00
        return factory;
    }

    private static decimal Parse(string value) =>
        decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
