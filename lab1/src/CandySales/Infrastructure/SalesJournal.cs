using System;
using System.Globalization;
using System.IO;
using System.Text;
using CandySales.Domain;

namespace CandySales.Infrastructure;

/// <summary>
/// Журнал продаж: дописывает каждую проведённую продажу строкой в CSV-файл (разделитель «;», UTF-8).
/// Класс владеет открытым файлом (FileStream внутри StreamWriter), поэтому реализует IDisposable:
/// файл нужно закрыть, чтобы освободить дескриптор ОС и снять блокировку на запись.
/// </summary>
public sealed class SalesJournal : IDisposable
{
    private const char Separator = ';';
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private static readonly string Header = string.Join(
        Separator,
        nameof(Sale.Id),
        nameof(Sale.SoldAt),
        nameof(Sale.CandyId),
        nameof(Sale.CandyName),
        nameof(Sale.QuantityKg),
        nameof(Sale.PricePerKg),
        nameof(Sale.Total),
        nameof(Sale.CustomerName));

    private readonly StreamWriter _writer;
    private bool _disposed;

    public SalesJournal(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FilePath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var isNewFile = !File.Exists(FilePath) || new FileInfo(FilePath).Length == 0;
        var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);

        // BOM в начале нового файла помогает Excel в Windows распознать UTF-8 и кириллицу.
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
        {
            AutoFlush = true,
        };

        if (isNewFile)
        {
            try
            {
                _writer.WriteLine(Header);
            }
            catch (IOException)
            {
                _writer.Dispose();
                throw;
            }
        }
    }

    /// <summary>Путь по умолчанию: подкаталог journal рядом с исполняемым файлом приложения.</summary>
    public static string DefaultFilePath => Path.Combine(AppContext.BaseDirectory, "journal", "sales-journal.csv");

    public string FilePath { get; }

    public void Write(Sale sale)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(sale);

        var invariant = CultureInfo.InvariantCulture;
        string[] fields =
        [
            sale.Id.ToString(invariant),
            sale.SoldAt.ToString(DateTimeFormat, invariant),
            sale.CandyId.ToString(invariant),
            Escape(sale.CandyName),
            sale.QuantityKg.ToString("0.000", invariant),
            sale.PricePerKg.ToString("0.00", invariant),
            sale.Total.ToString("0.00", invariant),
            Escape(sale.CustomerName ?? string.Empty),
        ];

        _writer.WriteLine(string.Join(Separator, fields));
    }

    /// <summary>Закрывает файл. Повторный вызов безопасен.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writer.Dispose();
        _disposed = true;
    }

    private static string Escape(string value) =>
        value.IndexOfAny([Separator, '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
