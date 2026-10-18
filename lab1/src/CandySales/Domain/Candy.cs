namespace CandySales.Domain;

/// <summary>Вид конфет, выпускаемых фабрикой, и его остаток на складе.</summary>
public sealed class Candy
{
    private readonly int _id;
    private string _name = string.Empty;
    private CandyCategory _category;
    private decimal _pricePerKg;
    private decimal _stockKg;

    /// <summary>Код товара. Задаётся один раз при создании (init) и далее не меняется.</summary>
    public required int Id
    {
        get => _id;
        init => _id = value > 0 ? value : throw new BusinessRuleException("Код конфет должен быть положительным числом.");
    }

    public required string Name
    {
        get => _name;
        set => _name = DomainValidation.RequireText(value, "Название конфет");
    }

    public required CandyCategory Category
    {
        get => _category;
        set => _category = DomainValidation.RequireCategory(value);
    }

    /// <summary>Текущая отпускная цена за килограмм, руб.</summary>
    public required decimal PricePerKg
    {
        get => _pricePerKg;
        set => _pricePerKg = DomainValidation.RequirePrice(value);
    }

    /// <summary>
    /// Остаток на складе, кг. Начальное значение задаётся через init,
    /// дальше остаток меняется только методами выпуска партии и продажи.
    /// </summary>
    public decimal StockKg
    {
        get => _stockKg;
        init => _stockKg = DomainValidation.RequireStock(value);
    }

    public bool IsInStock => StockKg > 0;

    /// <summary>Поступление на склад новой произведённой партии.</summary>
    public void AddStock(decimal quantityKg)
    {
        DomainValidation.RequireQuantity(quantityKg);
        _stockKg = DomainValidation.RequireStock(_stockKg + quantityKg);
    }

    /// <summary>Списание со склада при продаже. Вызывается только фабрикой после всех проверок.</summary>
    internal void RemoveStock(decimal quantityKg)
    {
        DomainValidation.RequireQuantity(quantityKg);
        if (quantityKg > _stockKg)
        {
            throw new BusinessRuleException(
                $"Недостаточно конфет «{Name}» на складе: запрошено {quantityKg:0.###} кг, в наличии {_stockKg:0.###} кг.");
        }

        _stockKg -= quantityKg;
    }
}
