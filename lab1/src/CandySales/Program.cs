using System;
using System.Globalization;
using System.IO;
using System.Text;
using CandySales.ConsoleUi;
using CandySales.Domain;
using CandySales.Infrastructure;

namespace CandySales;

internal static class Program
{
    private static int Main()
    {
        ConfigureConsole();

        var factory = new ConfectioneryFactory(DemoData.FactoryName);
        DemoData.Seed(factory);

        // using-объявление: журнал будет закрыт (Dispose) при выходе из Main, даже если возникнет исключение.
        // Журнал подключается после загрузки демонстрационных данных, поэтому в файл попадают только новые продажи.
        using var journal = TryOpenJournal(SalesJournal.DefaultFilePath);
        if (journal is not null)
        {
            factory.SaleCompleted += (_, e) => WriteToJournal(journal, e.Sale);
        }

        var app = new SalesConsoleApp(factory, journal?.FilePath);
        try
        {
            app.Run();
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine();
            Console.WriteLine("Входной поток закрыт. Работа приложения завершена.");
        }

        return 0;
    }

    private static void ConfigureConsole()
    {
        ConfigureEncoding();

        // Единый формат чисел и дат (запятая в дробях, ДД.ММ.ГГГГ) независимо от региональных настроек ОС.
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo("ru-RU");
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        // Ctrl+C завершает процесс; журнал пишется с AutoFlush, поэтому проведённые продажи уже сохранены.
        Console.CancelKeyPress += (_, _) =>
        {
            Console.WriteLine();
            Console.WriteLine("Получен Ctrl+C. Работа приложения прервана, проведённые продажи уже записаны в журнал.");
        };
    }

    /// <summary>
    /// Кириллица должна читаться и выводиться одинаково в Linux и Windows независимо от кодовой страницы терминала.
    /// </summary>
    private static void ConfigureEncoding()
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        // Ввод из файла или конвейера всегда читается как UTF-8 (BOM, если он есть, распознаётся автоматически).
        if (Console.IsInputRedirected)
        {
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), utf8));
        }

        // В консоли Windows кодировка UTF-16 включает Unicode-функции ReadConsoleW/WriteConsoleW,
        // поэтому русский текст не зависит от кодовой страницы (866, 1251, 437 и т. п.).
        // Перенаправленный вывод (в файл, в конвейер) и терминалы Linux/macOS получают UTF-8 без BOM.
        var isWindowsConsole = OperatingSystem.IsWindows();
        try
        {
            if (isWindowsConsole && !Console.IsInputRedirected)
            {
                Console.InputEncoding = Encoding.Unicode;
            }

            Console.OutputEncoding = isWindowsConsole && !Console.IsOutputRedirected ? Encoding.Unicode : utf8;
        }
        catch (IOException)
        {
            // Консоль не поддерживает выбранную кодировку — используем UTF-8.
            Console.OutputEncoding = utf8;
        }
    }

    private static SalesJournal? TryOpenJournal(string path)
    {
        try
        {
            return new SalesJournal(path);
        }
        catch (IOException ex)
        {
            ConsoleView.WriteWarning($"Журнал продаж недоступен ({ex.Message}). Приложение работает без журнала.");
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleView.WriteWarning($"Нет прав на запись журнала продаж ({ex.Message}). Приложение работает без журнала.");
        }

        return null;
    }

    private static void WriteToJournal(SalesJournal journal, Sale sale)
    {
        try
        {
            journal.Write(sale);
        }
        catch (IOException ex)
        {
            ConsoleView.WriteWarning($"Продажа № {sale.Id} проведена, но не записана в журнал: {ex.Message}");
        }
    }
}
