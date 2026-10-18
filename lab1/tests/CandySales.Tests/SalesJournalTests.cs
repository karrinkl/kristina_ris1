using System;
using System.IO;
using CandySales.Domain;
using CandySales.Infrastructure;
using Xunit;

namespace CandySales.Tests;

public sealed class SalesJournalTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "candy-sales-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Write_AppendsHeaderAndSaleLine()
    {
        var path = Path.Combine(_directory, "journal.csv");
        using (var journal = new SalesJournal(path))
        {
            journal.Write(CreateSale(1, "Карамель; мятная", null));
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Id;SoldAt;CandyId;CandyName;QuantityKg;PricePerKg;Total;CustomerName", lines[0]);
        Assert.Equal("1;2025-03-15 12:00:00;7;\"Карамель; мятная\";2.500;480.40;1201.00;", lines[1]);
    }

    [Fact]
    public void ReopenedJournal_AppendsWithoutSecondHeader()
    {
        var path = Path.Combine(_directory, "journal.csv");
        using (var journal = new SalesJournal(path))
        {
            journal.Write(CreateSale(1, "Ириска", "ООО «Продторг»"));
        }

        using (var journal = new SalesJournal(path))
        {
            journal.Write(CreateSale(2, "Ириска", null));
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("2;", lines[2]);
    }

    [Fact]
    public void Dispose_ReleasesFileAndForbidsFurtherWrites()
    {
        var path = Path.Combine(_directory, "journal.csv");
        var journal = new SalesJournal(path);

        journal.Dispose();
        journal.Dispose(); // повторный вызов безопасен

        Assert.Throws<ObjectDisposedException>(() => journal.Write(CreateSale(1, "Ириска", null)));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); // файл больше не заблокирован
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static Sale CreateSale(int id, string candyName, string? customer) => new()
    {
        Id = id,
        CandyId = 7,
        CandyName = candyName,
        QuantityKg = 2.5m,
        PricePerKg = 480.40m,
        SoldAt = TestFactory.Now,
        CustomerName = customer,
    };
}
