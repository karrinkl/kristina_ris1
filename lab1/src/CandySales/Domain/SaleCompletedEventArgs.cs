using System;

namespace CandySales.Domain;

/// <summary>Данные события об успешно оформленной продаже.</summary>
public sealed class SaleCompletedEventArgs : EventArgs
{
    public SaleCompletedEventArgs(Sale sale, decimal remainingStockKg)
    {
        Sale = sale;
        RemainingStockKg = remainingStockKg;
    }

    public Sale Sale { get; }

    /// <summary>Остаток проданных конфет на складе после продажи, кг.</summary>
    public decimal RemainingStockKg { get; }

    public bool IsStockLow => RemainingStockKg < ConfectioneryFactory.LowStockThresholdKg;
}
