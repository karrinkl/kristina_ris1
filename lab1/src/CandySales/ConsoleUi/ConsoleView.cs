using System;
using System.Collections.Generic;
using CandySales.Domain;

namespace CandySales.ConsoleUi;

/// <summary>Форматированный вывод в консоль: таблицы, сообщения, денежные суммы и массы.</summary>
internal static class ConsoleView
{
    private const string CandyHeader =
        " Код | Название                       | Категория  | Цена, руб/кг | Остаток, кг | Состояние";

    private const string SaleHeader =
        "   № | Дата и время     | Конфеты                        | Масса, кг | Цена, руб/кг |  Сумма, руб | Покупатель";

    public static string Money(decimal value) => $"{value:N2} руб.";

    public static string Kg(decimal value) => $"{value:0.###} кг";

    public static void WriteTitle(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    public static void WriteSuccess(string message) => WriteColored(message, ConsoleColor.Green);

    public static void WriteWarning(string message) => WriteColored($"[!] {message}", ConsoleColor.Yellow);

    public static void WriteError(string message) => WriteColored($"Ошибка: {message}", ConsoleColor.Red);

    /// <summary>Печатает строки таблицы; способ вывода одной строки передаётся делегатом Action.</summary>
    /// <returns>Количество выведенных строк.</returns>
    public static int PrintRows<T>(IEnumerable<T> items, string header, Action<T> printRow, string emptyMessage)
    {
        var count = 0;
        foreach (var item in items)
        {
            if (count == 0)
            {
                Console.WriteLine(header);
                Console.WriteLine(new string('-', header.Length));
            }

            printRow(item);
            count++;
        }

        Console.WriteLine(count == 0 ? emptyMessage : $"Всего записей: {count}");
        return count;
    }

    public static int PrintCandies(IEnumerable<Candy> candies, string emptyMessage = "Конфеты не найдены.") =>
        PrintRows(candies, CandyHeader, PrintCandyRow, emptyMessage);

    public static int PrintSales(IEnumerable<Sale> sales, string emptyMessage = "Продажи не найдены.") =>
        PrintRows(sales, SaleHeader, PrintSaleRow, emptyMessage);

    public static void PrintCandyDetails(Candy candy) =>
        Console.WriteLine(
            $"[{candy.Id}] {candy.Name}; категория: {candy.Category.ToDisplayName()}; " +
            $"цена: {Money(candy.PricePerKg)} за кг; остаток: {Kg(candy.StockKg)}");

    public static void PrintReceipt(Sale sale)
    {
        Console.WriteLine($"Продажа № {sale.Id} от {sale.SoldAt:dd.MM.yyyy HH:mm}");
        Console.WriteLine($"  Конфеты:    {sale.CandyName}");
        Console.WriteLine($"  Количество: {Kg(sale.QuantityKg)} × {Money(sale.PricePerKg)} за кг");
        Console.WriteLine($"  Покупатель: {sale.CustomerName ?? "розничный покупатель"}");
        Console.WriteLine($"  ИТОГО:      {Money(sale.Total)}");
    }

    /// <summary>Текстовое состояние остатка: switch-выражение с константным и относительным шаблонами.</summary>
    public static string DescribeStock(decimal stockKg) => stockKg switch
    {
        0m => "нет в наличии",
        < ConfectioneryFactory.LowStockThresholdKg => "заканчивается",
        _ => "в наличии",
    };

    private static void PrintCandyRow(Candy candy) =>
        Console.WriteLine(
            $"{candy.Id,4} | {Fit(candy.Name, 30),-30} | {candy.Category.ToDisplayName(),-10} | " +
            $"{candy.PricePerKg,12:N2} | {candy.StockKg,11:0.000} | {DescribeStock(candy.StockKg)}");

    private static void PrintSaleRow(Sale sale) =>
        Console.WriteLine(
            $"{sale.Id,4} | {sale.SoldAt,-16:dd.MM.yyyy HH:mm} | {Fit(sale.CandyName, 30),-30} | " +
            $"{sale.QuantityKg,9:0.000} | {sale.PricePerKg,12:N2} | {sale.Total,11:N2} | {sale.CustomerName ?? "—"}");

    private static string Fit(string text, int width) =>
        text.Length <= width ? text : string.Concat(text.AsSpan(0, width - 1), "…");

    private static void WriteColored(string message, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = previous;
    }
}
