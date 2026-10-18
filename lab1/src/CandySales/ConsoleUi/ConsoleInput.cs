using System;
using System.IO;
using CandySales.Domain;

namespace CandySales.ConsoleUi;

/// <summary>
/// Чтение данных с клавиатуры с повторным запросом при ошибке.
/// Некорректный ввод не приводит к аварийному завершению: пользователь видит сообщение и вводит значение снова.
/// </summary>
internal static class ConsoleInput
{
    /// <summary>Читает строку. Если входной поток закрыт (Ctrl+D / Ctrl+Z), выбрасывает EndOfStreamException.</summary>
    public static string ReadLine(string prompt)
    {
        Console.Write(prompt);
        var line = Console.ReadLine() ?? throw new EndOfStreamException("Входной поток закрыт.");

        // При вводе из файла (перенаправление) повторяем введённое, чтобы протокол работы был читаемым.
        if (Console.IsInputRedirected)
        {
            Console.WriteLine(line);
        }

        return line;
    }

    /// <summary>
    /// Запрашивает значение, пока функция <paramref name="parse"/> не вернёт результат.
    /// Ошибки формата и нарушения правил предметной области показываются пользователю.
    /// </summary>
    public static T Read<T>(string prompt, Func<string, T> parse)
    {
        while (true)
        {
            var text = ReadLine(prompt);
            try
            {
                return parse(text);
            }
            catch (FormatException ex)
            {
                ConsoleView.WriteError(ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                ConsoleView.WriteError(ex.Message);
            }
        }
    }

    /// <summary>
    /// Необязательное значение: пустой ввод (Enter) возвращает null — «оставить как есть».
    /// Возвращаемый тип T? здесь — nullable value type (Nullable&lt;T&gt;).
    /// </summary>
    public static T? ReadOptional<T>(string prompt, Func<string, T> parse)
        where T : struct =>
        Read<T?>(prompt, text => string.IsNullOrWhiteSpace(text) ? null : parse(text));

    public static string? ReadOptionalText(string prompt)
    {
        var text = ReadLine(prompt).Trim();
        return text.Length == 0 ? null : text;
    }

    public static int ReadInt(string prompt, int min, int max) =>
        Read(prompt, text =>
        {
            var value = InputParser.ParseInt(text);
            return value >= min && value <= max
                ? value
                : throw new FormatException($"Введите число от {min} до {max}.");
        });

    public static DateOnly ReadDate(string prompt) => Read(prompt, InputParser.ParseDate);

    public static bool Confirm(string prompt) => Read($"{prompt} (д/н): ", InputParser.ParseYesNo);
}
