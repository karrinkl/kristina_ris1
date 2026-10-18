using System;
using CandySales.ConsoleUi;
using Xunit;

namespace CandySales.Tests;

public class InputParserTests
{
    [Theory]
    [InlineData("12,5", "12.5")]
    [InlineData("12.5", "12.5")]
    [InlineData(" 7 ", "7")]
    [InlineData("-3,25", "-3.25")]
    public void ParseDecimal_AcceptsCommaAndDot(string input, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), InputParser.ParseDecimal(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1 000")]
    [InlineData("1,2,3")]
    [InlineData("1e5")]
    public void ParseDecimal_RejectsInvalidText(string input)
    {
        Assert.Throws<FormatException>(() => InputParser.ParseDecimal(input));
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData(" -1 ", -1)]
    public void ParseInt_AcceptsIntegers(string input, int expected)
    {
        Assert.Equal(expected, InputParser.ParseInt(input));
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("пять")]
    [InlineData("99999999999")]
    public void ParseInt_RejectsInvalidText(string input)
    {
        Assert.Throws<FormatException>(() => InputParser.ParseInt(input));
    }

    [Fact]
    public void ParseDate_AcceptsDayMonthYear()
    {
        Assert.Equal(new DateOnly(2025, 3, 5), InputParser.ParseDate("05.03.2025"));
    }

    [Theory]
    [InlineData("31.02.2025")]
    [InlineData("2025-03-05")]
    [InlineData("5.3.2025")]
    [InlineData("вчера")]
    public void ParseDate_RejectsInvalidDates(string input)
    {
        Assert.Throws<FormatException>(() => InputParser.ParseDate(input));
    }

    [Theory]
    [InlineData("д", true)]
    [InlineData("ДА", true)]
    [InlineData("n", false)]
    [InlineData("нет", false)]
    public void ParseYesNo_RecognizesAnswers(string input, bool expected)
    {
        Assert.Equal(expected, InputParser.ParseYesNo(input));
    }

    [Fact]
    public void ParseYesNo_RejectsUnknownAnswer()
    {
        Assert.Throws<FormatException>(() => InputParser.ParseYesNo("может быть"));
    }
}
