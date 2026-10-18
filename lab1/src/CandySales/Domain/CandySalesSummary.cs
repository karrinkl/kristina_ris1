namespace CandySales.Domain;

/// <summary>Итоги продаж одного вида конфет (строка аналитического отчёта).</summary>
public readonly record struct CandySalesSummary(
    int CandyId,
    string CandyName,
    decimal TotalKg,
    decimal Revenue,
    int SalesCount);
