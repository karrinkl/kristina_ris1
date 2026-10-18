using System;

namespace CandySales.Domain;

/// <summary>
/// Проведённая продажа. Объект неизменяемый: все свойства задаются только при создании (init),
/// а цена и название конфет копируются на момент продажи, поэтому дальнейшее изменение
/// карточки товара не влияет на историю.
/// </summary>
public sealed class Sale
{
    private readonly decimal _quantityKg;
    private readonly decimal _pricePerKg;

    public required int Id { get; init; }

    public required int CandyId { get; init; }

    /// <summary>Название конфет на момент продажи.</summary>
    public required string CandyName { get; init; }

    public required decimal QuantityKg
    {
        get => _quantityKg;
        init => _quantityKg = DomainValidation.RequireQuantity(value);
    }

    /// <summary>Цена за килограмм, действовавшая на момент продажи.</summary>
    public required decimal PricePerKg
    {
        get => _pricePerKg;
        init => _pricePerKg = DomainValidation.RequirePrice(value);
    }

    public required DateTime SoldAt { get; init; }

    /// <summary>Покупатель (магазин, оптовый клиент). Null — розничная продажа без указания покупателя.</summary>
    public string? CustomerName { get; init; }

    /// <summary>Итоговая сумма, руб.</summary>
    public decimal Total => CalculateTotal(QuantityKg, PricePerKg);

    /// <summary>Стоимость: масса × цена, округлённая до копеек по правилам денежного округления.</summary>
    public static decimal CalculateTotal(decimal quantityKg, decimal pricePerKg) =>
        Math.Round(quantityKg * pricePerKg, 2, MidpointRounding.AwayFromZero);
}
