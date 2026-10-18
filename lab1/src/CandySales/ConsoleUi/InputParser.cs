using System;
using System.Globalization;

namespace CandySales.ConsoleUi;

/// <summary>
/// Разбор строк, введённых пользователем. Методы не зависят от настроек культуры ОС:
/// дробную часть можно отделять и запятой, и точкой, даты вводятся в формате ДД.ММ.ГГГГ.
/// При ошибке формата выбрасывается FormatException с понятным сообщением.
/// </summary>
internal static class InputParser
{
    public const string DateFormat = "dd.MM.yyyy";

    private const NumberStyles DecimalStyle =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static int ParseInt(string text)
    {
        if (int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException($"«{text.Trim()}» не является целым числом.");
    }

    public static decimal ParseDecimal(string text)
    {
        var normalized = text.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, DecimalStyle, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException($"«{text.Trim()}» не является числом. Пример правильного ввода: 12,5 или 12.5");
    }

    public static DateOnly ParseDate(string text)
    {
        if (DateOnly.TryParseExact(text.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        throw new FormatException($"«{text.Trim()}» не является корректной датой. Используйте формат ДД.ММ.ГГГГ, например 05.03.2025.");
    }

    public static bool ParseYesNo(string text) => text.Trim().ToLowerInvariant() switch
    {
        "д" or "да" or "y" or "yes" => true,
        "н" or "нет" or "n" or "no" => false,
        var other => throw new FormatException($"Ответ «{other}» не распознан. Введите «д» (да) или «н» (нет)."),
    };
}
