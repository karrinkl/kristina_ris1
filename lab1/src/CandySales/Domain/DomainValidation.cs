using System;

namespace CandySales.Domain;

/// <summary>Общие правила проверки значений предметной области.</summary>
internal static class DomainValidation
{
    public const int MaxTextLength = 60;
    public const decimal MaxPricePerKg = 100_000m;
    public const decimal MaxStockKg = 1_000_000m;

    public static string RequireText(string? value, string fieldTitle)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleException($"{fieldTitle}: значение не может быть пустым.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxTextLength)
        {
            throw new BusinessRuleException($"{fieldTitle}: допускается не более {MaxTextLength} символов.");
        }

        return trimmed;
    }

    /// <summary>Пустая строка означает «значение не указано».</summary>
    public static string? OptionalText(string? value, string fieldTitle) =>
        string.IsNullOrWhiteSpace(value) ? null : RequireText(value, fieldTitle);

    public static CandyCategory RequireCategory(CandyCategory category) =>
        Enum.IsDefined(category)
            ? category
            : throw new BusinessRuleException($"Категория {(int)category} не существует.");

    /// <summary>Цена за килограмм: больше нуля, в рублях с точностью до копейки.</summary>
    public static decimal RequirePrice(decimal pricePerKg)
    {
        if (pricePerKg <= 0)
        {
            throw new BusinessRuleException("Цена за килограмм должна быть больше нуля.");
        }

        if (pricePerKg > MaxPricePerKg)
        {
            throw new BusinessRuleException($"Цена за килограмм не может превышать {MaxPricePerKg:N0} руб.");
        }

        if (decimal.Round(pricePerKg, 2) != pricePerKg)
        {
            throw new BusinessRuleException("Цена указывается в рублях с точностью до копеек (не более 2 знаков после запятой).");
        }

        return pricePerKg;
    }

    /// <summary>Количество для продажи или выпуска партии: больше нуля, с точностью до грамма.</summary>
    public static decimal RequireQuantity(decimal quantityKg)
    {
        if (quantityKg <= 0)
        {
            throw new BusinessRuleException("Количество должно быть больше нуля.");
        }

        return RequireWeightLimits(quantityKg);
    }

    /// <summary>Остаток на складе: ноль допустим, отрицательное значение — нет.</summary>
    public static decimal RequireStock(decimal stockKg)
    {
        if (stockKg < 0)
        {
            throw new BusinessRuleException("Остаток на складе не может быть отрицательным.");
        }

        return RequireWeightLimits(stockKg);
    }

    private static decimal RequireWeightLimits(decimal weightKg)
    {
        if (weightKg > MaxStockKg)
        {
            throw new BusinessRuleException($"Масса не может превышать {MaxStockKg:N0} кг.");
        }

        if (decimal.Round(weightKg, 3) != weightKg)
        {
            throw new BusinessRuleException("Масса указывается в килограммах с точностью до грамма (не более 3 знаков после запятой).");
        }

        return weightKg;
    }
}
