using System;
using CandySales.Domain;
using Xunit;

namespace CandySales.Tests;

public class SalesPeriodTests
{
    [Fact]
    public void Constructor_WithEndBeforeStart_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => new SalesPeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 9)));
    }

    [Theory]
    [InlineData(2025, 3, 10, 0, 0, true)]
    [InlineData(2025, 3, 12, 23, 59, true)]
    [InlineData(2025, 3, 9, 23, 59, false)]
    [InlineData(2025, 3, 13, 0, 0, false)]
    public void Contains_IncludesBothBoundaries(int year, int month, int day, int hour, int minute, bool expected)
    {
        var period = new SalesPeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 12));

        Assert.Equal(expected, period.Contains(new DateTime(year, month, day, hour, minute, 0)));
    }

    [Fact]
    public void Copy_IsIndependentValue()
    {
        var original = new SalesPeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        var copy = original;

        Assert.Equal(original, copy);
        Assert.Equal(original.From, copy.From);
    }
}
