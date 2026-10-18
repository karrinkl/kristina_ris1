using System;
using System.Collections.Generic;
using System.Linq;

namespace CandySales.Domain;

/// <summary>
/// Кондитерская фабрика: ведёт ассортимент конфет, остатки на складе и историю продаж.
/// Класс не зависит от консоли, поэтому вся бизнес-логика проверяется автоматическими тестами.
/// </summary>
public sealed class ConfectioneryFactory
{
    /// <summary>Порог, ниже которого остаток считается заканчивающимся, кг.</summary>
    public const decimal LowStockThresholdKg = 20m;

    private readonly Dictionary<int, Candy> _candies = new();
    private readonly List<Sale> _sales = new();
    private readonly Func<DateTime> _clock;
    private int _nextCandyId = 1;
    private int _nextSaleId = 1;

    /// <param name="name">Название фабрики.</param>
    /// <param name="clock">Источник текущего времени; в тестах подменяется фиксированной датой.</param>
    public ConfectioneryFactory(string name, Func<DateTime>? clock = null)
    {
        Name = DomainValidation.RequireText(name, "Название фабрики");
        _clock = clock ?? (() => DateTime.Now);
    }

    /// <summary>Возникает после успешного оформления продажи и списания товара со склада.</summary>
    public event EventHandler<SaleCompletedEventArgs>? SaleCompleted;

    public string Name { get; }

    public int CandyCount => _candies.Count;

    public int SaleCount => _sales.Count;

    public DateTime Now => _clock();

    // ---------- Ассортимент ----------

    public IEnumerable<Candy> GetCandies() => _candies.Values.OrderBy(candy => candy.Id);

    public Candy? FindCandy(int id) => _candies.TryGetValue(id, out var candy) ? candy : null;

    public Candy GetCandy(int id) =>
        FindCandy(id) ?? throw new BusinessRuleException($"Конфеты с кодом {id} не найдены.");

    public Candy AddCandy(string name, CandyCategory category, decimal pricePerKg, decimal stockKg)
    {
        var candy = new Candy
        {
            Id = _nextCandyId,
            Name = name,
            Category = category,
            PricePerKg = pricePerKg,
            StockKg = stockKg,
        };

        EnsureNameIsUnique(candy.Name, exceptCandyId: null);
        _candies.Add(candy.Id, candy);
        _nextCandyId++;
        return candy;
    }

    /// <summary>
    /// Изменяет карточку товара. Параметр со значением null означает «оставить без изменений».
    /// Сначала проверяются все новые значения, затем они применяются, поэтому частичного изменения не бывает.
    /// </summary>
    public void UpdateCandy(int id, string? newName = null, CandyCategory? newCategory = null, decimal? newPricePerKg = null)
    {
        var candy = GetCandy(id);

        var name = newName is null ? null : DomainValidation.RequireText(newName, "Название конфет");
        if (name is not null)
        {
            EnsureNameIsUnique(name, exceptCandyId: id);
        }

        if (newCategory is CandyCategory category)
        {
            DomainValidation.RequireCategory(category);
        }

        if (newPricePerKg is decimal price)
        {
            DomainValidation.RequirePrice(price);
        }

        candy.Name = name ?? candy.Name;
        candy.Category = newCategory ?? candy.Category;
        candy.PricePerKg = newPricePerKg ?? candy.PricePerKg;
    }

    /// <summary>Поступление на склад новой партии, произведённой фабрикой.</summary>
    public void AddProducedBatch(int candyId, decimal quantityKg) => GetCandy(candyId).AddStock(quantityKg);

    /// <summary>Удаляет вид конфет. Запрещено, если по нему есть продажи: история должна оставаться целостной.</summary>
    public void RemoveCandy(int id)
    {
        var candy = GetCandy(id);
        if (HasSales(id))
        {
            throw new BusinessRuleException(
                $"Нельзя удалить «{candy.Name}»: по этому виду конфет уже есть продажи. Удаление нарушило бы историю продаж.");
        }

        _candies.Remove(id);
    }

    public bool HasSales(int candyId)
    {
        foreach (var sale in _sales)
        {
            if (sale.CandyId == candyId)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<Candy> SearchByName(string fragment)
    {
        var text = fragment.Trim();
        return GetCandies().Where(candy => candy.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Фильтрация по произвольному условию, переданному делегатом Predicate.</summary>
    public List<Candy> FilterCandies(Predicate<Candy> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var candies = new List<Candy>(GetCandies());
        return candies.FindAll(predicate);
    }

    public IEnumerable<Candy> SortCandies(CandySortField field, bool descending = false)
    {
        var candies = _candies.Values;
        return field switch
        {
            CandySortField.Name => candies.OrderBy(candy => candy.Name, WithDirection(StringComparer.CurrentCultureIgnoreCase, descending)),
            CandySortField.Price => candies.OrderBy(candy => candy.PricePerKg, WithDirection(Comparer<decimal>.Default, descending)),
            CandySortField.Stock => candies.OrderBy(candy => candy.StockKg, WithDirection(Comparer<decimal>.Default, descending)),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Неизвестное поле сортировки."),
        };
    }

    // ---------- Продажи ----------

    /// <summary>
    /// Оформляет продажу: проверяет количество и остаток, фиксирует цену на момент продажи,
    /// списывает товар со склада и уведомляет подписчиков событием <see cref="SaleCompleted"/>.
    /// </summary>
    /// <param name="soldAt">Момент продажи; null — текущее время. Используется для начальных данных и тестов.</param>
    public Sale SellCandy(int candyId, decimal quantityKg, string? customerName = null, DateTime? soldAt = null)
    {
        DomainValidation.RequireQuantity(quantityKg);
        var candy = GetCandy(candyId);

        if (quantityKg > candy.StockKg)
        {
            throw new BusinessRuleException(
                $"Недостаточно конфет «{candy.Name}» на складе: запрошено {quantityKg:0.###} кг, в наличии {candy.StockKg:0.###} кг.");
        }

        var now = _clock();
        var moment = soldAt ?? now;
        if (moment > now)
        {
            throw new BusinessRuleException("Дата продажи не может быть в будущем.");
        }

        var sale = new Sale
        {
            Id = _nextSaleId,
            CandyId = candy.Id,
            CandyName = candy.Name,
            QuantityKg = quantityKg,
            PricePerKg = candy.PricePerKg,
            SoldAt = moment,
            CustomerName = DomainValidation.OptionalText(customerName, "Покупатель"),
        };

        candy.RemoveStock(quantityKg);
        _sales.Add(sale);
        _nextSaleId++;

        SaleCompleted?.Invoke(this, new SaleCompletedEventArgs(sale, candy.StockKg));
        return sale;
    }

    /// <summary>Все продажи в хронологическом порядке.</summary>
    public IEnumerable<Sale> GetSales() => _sales.OrderBy(sale => sale.SoldAt);

    public IEnumerable<Sale> FindSalesByDate(DateOnly date) =>
        FindSales(sale => DateOnly.FromDateTime(sale.SoldAt) == date);

    public IEnumerable<Sale> FindSalesInPeriod(SalesPeriod period) =>
        FindSales(sale => period.Contains(sale.SoldAt));

    public IEnumerable<Sale> FindSalesByCandyName(string fragment)
    {
        var text = fragment.Trim();
        return FindSales(sale => sale.CandyName.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Sale> FindSalesByCustomer(string fragment)
    {
        var text = fragment.Trim();
        return FindSales(sale => sale.CustomerName is { } customer
            && customer.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    public decimal CalculateRevenue(SalesPeriod period) => SumTotals(FindSalesInPeriod(period));

    public decimal CalculateTotalRevenue() => SumTotals(_sales);

    /// <summary>Самые продаваемые конфеты по проданной массе (по убыванию).</summary>
    public CandySalesSummary[] GetTopSellingCandies(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var totals = new Dictionary<int, CandySalesSummary>();
        foreach (var sale in _sales)
        {
            totals[sale.CandyId] = totals.TryGetValue(sale.CandyId, out var summary)
                ? summary with
                {
                    TotalKg = summary.TotalKg + sale.QuantityKg,
                    Revenue = summary.Revenue + sale.Total,
                    SalesCount = summary.SalesCount + 1,
                }
                : new CandySalesSummary(sale.CandyId, FindCandy(sale.CandyId)?.Name ?? sale.CandyName, sale.QuantityKg, sale.Total, 1);
        }

        var ordered = totals.Values.OrderBy(summary => summary.TotalKg, WithDirection(Comparer<decimal>.Default, descending: true));
        var top = new CandySalesSummary[Math.Min(count, totals.Count)];
        var index = 0;
        foreach (var summary in ordered)
        {
            if (index == top.Length)
            {
                break;
            }

            top[index++] = summary;
        }

        return top;
    }

    private IEnumerable<Sale> FindSales(Func<Sale, bool> criterion) => GetSales().Where(criterion);

    private static decimal SumTotals(IEnumerable<Sale> sales)
    {
        var sum = 0m;
        foreach (var sale in sales)
        {
            sum += sale.Total;
        }

        return sum;
    }

    private void EnsureNameIsUnique(string name, int? exceptCandyId)
    {
        foreach (var candy in _candies.Values)
        {
            if (candy.Id != exceptCandyId && string.Equals(candy.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException($"Конфеты с названием «{name}» уже есть в ассортименте (код {candy.Id}).");
            }
        }
    }

    private static IComparer<T> WithDirection<T>(IComparer<T> comparer, bool descending) =>
        descending ? Comparer<T>.Create((x, y) => comparer.Compare(y, x)) : comparer;
}
