using System;

namespace CandySales.Domain;

/// <summary>
/// Отчётный период (обе границы включительно). Небольшой неизменяемый тип-значение:
/// передаётся копированием и не требует выделения памяти в куче.
/// </summary>
public readonly struct SalesPeriod
{
    public SalesPeriod(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new BusinessRuleException("Дата окончания периода не может быть раньше даты начала.");
        }

        From = from;
        To = to;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public bool Contains(DateTime moment)
    {
        var date = DateOnly.FromDateTime(moment);
        return date >= From && date <= To;
    }

    public override string ToString() => $"{From:dd.MM.yyyy} — {To:dd.MM.yyyy}";
}
