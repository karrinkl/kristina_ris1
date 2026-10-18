using System;
using System.Collections.Generic;
using CandySales.Domain;

namespace CandySales.ConsoleUi;

/// <summary>
/// Интерактивное меню приложения. Отвечает только за диалог с пользователем;
/// все операции и проверки правил выполняет <see cref="ConfectioneryFactory"/>.
/// </summary>
internal sealed class SalesConsoleApp
{
    private const string ExitKey = "0";

    private readonly ConfectioneryFactory _factory;
    private readonly string? _journalPath;
    private readonly MenuItem[] _menu;
    private readonly Dictionary<string, MenuItem> _menuByKey = new();

    public SalesConsoleApp(ConfectioneryFactory factory, string? journalPath)
    {
        _factory = factory;
        _journalPath = journalPath;
        _menu =
        [
            new("1", "Показать весь ассортимент", ShowAssortment),
            new("2", "Добавить новый вид конфет", AddCandy),
            new("3", "Изменить вид конфет", EditCandy),
            new("4", "Принять на склад новую партию", AddProducedBatch),
            new("5", "Удалить вид конфет", RemoveCandy),
            new("6", "Найти конфеты по названию", SearchCandies),
            new("7", "Отфильтровать ассортимент", FilterCandies),
            new("8", "Отсортировать ассортимент", SortCandies),
            new("9", "Оформить продажу", SellCandy),
            new("10", "Показать все продажи", ShowSales),
            new("11", "Найти продажи", FindSales),
            new("12", "Рассчитать выручку", ShowRevenue),
            new("13", "Самые продаваемые конфеты", ShowTopSellers),
            new("14", "Демонстрация возможностей C# (типы, boxing, nullable)", () => CSharpFeaturesDemo.Run(Console.Out)),
        ];

        foreach (var item in _menu)
        {
            _menuByKey.Add(item.Key, item);
        }

        _factory.SaleCompleted += OnSaleCompleted;
    }

    public void Run()
    {
        Console.WriteLine($"Учёт продажи конфет — {_factory.Name}");
        Console.WriteLine($"Загружены демонстрационные данные: видов конфет — {_factory.CandyCount}, продаж — {_factory.SaleCount}.");
        if (_journalPath is not null)
        {
            Console.WriteLine($"Новые продажи записываются в журнал: {_journalPath}");
        }

        while (true)
        {
            PrintMenu();
            var key = ConsoleInput.ReadLine("Выберите пункт меню: ").Trim();
            if (key == ExitKey)
            {
                Console.WriteLine("Работа завершена. До свидания!");
                return;
            }

            if (_menuByKey.TryGetValue(key, out var item))
            {
                Execute(item);
            }
            else
            {
                ConsoleView.WriteError($"Пункта меню «{key}» нет. Введите номер из списка.");
            }
        }
    }

    private void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("──────────────── ГЛАВНОЕ МЕНЮ ────────────────");
        foreach (var item in _menu)
        {
            Console.WriteLine($"{item.Key,3}. {item.Title}");
        }

        Console.WriteLine($"{ExitKey,3}. Выход");
    }

    /// <summary>Выполняет пункт меню; ожидаемые ошибки предметной области не прерывают работу программы.</summary>
    private static void Execute(MenuItem item)
    {
        ConsoleView.WriteTitle(item.Title);
        try
        {
            item.Execute();
        }
        catch (BusinessRuleException ex)
        {
            ConsoleView.WriteError(ex.Message);
        }
    }

    private void OnSaleCompleted(object? sender, SaleCompletedEventArgs e)
    {
        Console.WriteLine($"Склад: остаток «{e.Sale.CandyName}» после продажи — {ConsoleView.Kg(e.RemainingStockKg)}.");
        if (e.IsStockLow)
        {
            ConsoleView.WriteWarning(
                $"Остаток ниже {ConsoleView.Kg(ConfectioneryFactory.LowStockThresholdKg)}. Запланируйте выпуск новой партии.");
        }
    }

    // ---------- Ассортимент ----------

    private void ShowAssortment() =>
        ConsoleView.PrintCandies(_factory.GetCandies(), "Ассортимент пуст.");

    private void AddCandy()
    {
        var name = ConsoleInput.Read("Название: ", text => DomainValidation.RequireText(text, "Название конфет"));
        var category = ReadCategory();
        var price = ConsoleInput.Read("Цена за 1 кг, руб.: ", ParsePrice);
        var stock = ConsoleInput.Read("Начальный остаток на складе, кг: ",
            text => DomainValidation.RequireStock(InputParser.ParseDecimal(text)));

        var candy = _factory.AddCandy(name, category, price, stock);
        ConsoleView.WriteSuccess($"Добавлено в ассортимент под кодом {candy.Id}:");
        ConsoleView.PrintCandyDetails(candy);
    }

    private void EditCandy()
    {
        if (ReadCandyOrCancel("Код конфет для изменения") is not { } candy)
        {
            return;
        }

        ConsoleView.PrintCandyDetails(candy);
        Console.WriteLine("Введите новые значения. Пустой ввод (Enter) оставляет значение без изменений.");

        var newName = ConsoleInput.ReadOptionalText($"Название [{candy.Name}]: ");
        var newCategory = ReadOptionalCategory(candy.Category);
        var newPrice = ConsoleInput.ReadOptional($"Цена за 1 кг [{candy.PricePerKg:N2}]: ", ParsePrice);

        if (newName is null && newCategory is null && newPrice is null)
        {
            Console.WriteLine("Изменений нет.");
            return;
        }

        _factory.UpdateCandy(candy.Id, newName, newCategory, newPrice);
        ConsoleView.WriteSuccess("Данные обновлены:");
        ConsoleView.PrintCandyDetails(candy);
        if (newPrice is not null && _factory.HasSales(candy.Id))
        {
            Console.WriteLine("Цены в уже проведённых продажах не изменились: в истории хранится цена на момент продажи.");
        }
    }

    private void AddProducedBatch()
    {
        if (ReadCandyOrCancel("Код конфет") is not { } candy)
        {
            return;
        }

        var quantity = ConsoleInput.Read("Масса произведённой партии, кг: ", ParseQuantity);
        _factory.AddProducedBatch(candy.Id, quantity);
        ConsoleView.WriteSuccess($"Партия принята. Остаток «{candy.Name}»: {ConsoleView.Kg(candy.StockKg)}.");
    }

    private void RemoveCandy()
    {
        if (ReadCandyOrCancel("Код конфет для удаления") is not { } candy)
        {
            return;
        }

        ConsoleView.PrintCandyDetails(candy);
        if (_factory.HasSales(candy.Id))
        {
            ConsoleView.WriteError($"«{candy.Name}» нельзя удалить: по этому виду конфет есть продажи, удаление нарушило бы историю.");
            return;
        }

        if (!ConsoleInput.Confirm("Удалить этот вид конфет из ассортимента?"))
        {
            Console.WriteLine("Удаление отменено.");
            return;
        }

        _factory.RemoveCandy(candy.Id);
        ConsoleView.WriteSuccess($"«{candy.Name}» удалены из ассортимента.");
    }

    private void SearchCandies()
    {
        var fragment = ReadSearchText("Часть названия: ");
        ConsoleView.PrintCandies(_factory.SearchByName(fragment), $"Конфеты, название которых содержит «{fragment}», не найдены.");
    }

    private void FilterCandies()
    {
        Console.WriteLine("1. По категории");
        Console.WriteLine("2. По диапазону цен");
        Console.WriteLine("3. Только имеющиеся на складе");
        Console.WriteLine($"4. Заканчивающиеся (остаток меньше {ConsoleView.Kg(ConfectioneryFactory.LowStockThresholdKg)})");
        var choice = ConsoleInput.ReadInt("Вариант фильтра: ", 1, 4);

        // Switch-выражение возвращает делегат Predicate<Candy>, созданный лямбда-выражением.
        Predicate<Candy> predicate = choice switch
        {
            1 => CreateCategoryFilter(),
            2 => CreatePriceFilter(),
            3 => candy => candy.IsInStock,
            _ => candy => candy.StockKg < ConfectioneryFactory.LowStockThresholdKg,
        };

        ConsoleView.PrintCandies(_factory.FilterCandies(predicate), "Подходящих конфет нет.");
    }

    private void SortCandies()
    {
        Console.WriteLine("1. По названию");
        Console.WriteLine("2. По цене");
        Console.WriteLine("3. По остатку на складе");
        var field = ConsoleInput.ReadInt("Поле сортировки: ", 1, 3) switch
        {
            1 => CandySortField.Name,
            2 => CandySortField.Price,
            _ => CandySortField.Stock,
        };
        var descending = ConsoleInput.Confirm("Сортировать по убыванию?");

        ConsoleView.PrintCandies(_factory.SortCandies(field, descending), "Ассортимент пуст.");
    }

    // ---------- Продажи ----------

    private void SellCandy()
    {
        if (ReadCandyOrCancel("Код продаваемых конфет") is not { } candy)
        {
            return;
        }

        if (!candy.IsInStock)
        {
            ConsoleView.WriteError($"«{candy.Name}» нет в наличии. Сначала примите на склад новую партию.");
            return;
        }

        ConsoleView.PrintCandyDetails(candy);
        var quantity = ConsoleInput.Read("Количество, кг: ", text =>
        {
            var value = ParseQuantity(text);
            return value <= candy.StockKg
                ? value
                : throw new BusinessRuleException($"На складе только {ConsoleView.Kg(candy.StockKg)}. Укажите меньшее количество.");
        });
        var customer = ConsoleInput.ReadOptionalText("Покупатель (Enter — розничная продажа): ");

        var total = Sale.CalculateTotal(quantity, candy.PricePerKg);
        Console.WriteLine($"Стоимость: {ConsoleView.Kg(quantity)} × {ConsoleView.Money(candy.PricePerKg)} = {ConsoleView.Money(total)}");
        if (!ConsoleInput.Confirm("Оформить продажу?"))
        {
            Console.WriteLine("Продажа отменена.");
            return;
        }

        var sale = _factory.SellCandy(candy.Id, quantity, customer);
        ConsoleView.WriteSuccess("Продажа оформлена.");
        ConsoleView.PrintReceipt(sale);
    }

    private void ShowSales()
    {
        if (ConsoleView.PrintSales(_factory.GetSales(), "Продаж пока нет.") > 0)
        {
            Console.WriteLine($"Общая выручка: {ConsoleView.Money(_factory.CalculateTotalRevenue())}");
        }
    }

    private void FindSales()
    {
        Console.WriteLine("1. По дате");
        Console.WriteLine("2. За период");
        Console.WriteLine("3. По названию конфет");
        Console.WriteLine("4. По покупателю");
        var choice = ConsoleInput.ReadInt("Критерий поиска: ", 1, 4);

        IEnumerable<Sale> found = choice switch
        {
            1 => _factory.FindSalesByDate(ConsoleInput.ReadDate("Дата (ДД.ММ.ГГГГ): ")),
            2 => _factory.FindSalesInPeriod(ReadPeriod()),
            3 => _factory.FindSalesByCandyName(ReadSearchText("Часть названия конфет: ")),
            _ => _factory.FindSalesByCustomer(ReadSearchText("Часть названия покупателя: ")),
        };

        ConsoleView.PrintSales(found);
    }

    private void ShowRevenue()
    {
        var today = DateOnly.FromDateTime(_factory.Now);
        Console.WriteLine("1. За выбранный период");
        Console.WriteLine("2. За сегодня");
        Console.WriteLine("3. За последние 7 дней");
        Console.WriteLine("4. Общая выручка за всё время");
        var choice = ConsoleInput.ReadInt("Вариант: ", 1, 4);

        // Nullable value type: для общей выручки период не задаётся.
        SalesPeriod? period = choice switch
        {
            1 => ReadPeriod(),
            2 => new SalesPeriod(today, today),
            3 => new SalesPeriod(today.AddDays(-6), today),
            _ => null,
        };

        if (period is { } selected)
        {
            var sales = new List<Sale>(_factory.FindSalesInPeriod(selected));
            Console.WriteLine($"Период: {selected}");
            Console.WriteLine($"Количество продаж: {sales.Count}");
            Console.WriteLine($"Выручка: {ConsoleView.Money(_factory.CalculateRevenue(selected))}");
        }
        else
        {
            Console.WriteLine($"Количество продаж: {_factory.SaleCount}");
            Console.WriteLine($"Общая выручка: {ConsoleView.Money(_factory.CalculateTotalRevenue())}");
        }
    }

    private void ShowTopSellers()
    {
        var count = ConsoleInput.ReadInt("Сколько позиций показать (1–10): ", 1, 10);
        CandySalesSummary[] top = _factory.GetTopSellingCandies(count);
        if (top.Length == 0)
        {
            Console.WriteLine("Продаж пока нет.");
            return;
        }

        Console.WriteLine("Место | Конфеты                        | Продано, кг | Продаж | Выручка, руб");
        Console.WriteLine(new string('-', 78));
        for (var i = 0; i < top.Length; i++)
        {
            var row = top[i];
            Console.WriteLine($"{i + 1,5} | {row.CandyName,-30} | {row.TotalKg,11:0.000} | {row.SalesCount,6} | {row.Revenue,12:N2}");
        }
    }

    // ---------- Вспомогательные методы ввода ----------

    private static decimal ParsePrice(string text) => DomainValidation.RequirePrice(InputParser.ParseDecimal(text));

    private static decimal ParseQuantity(string text) => DomainValidation.RequireQuantity(InputParser.ParseDecimal(text));

    private static string ReadSearchText(string prompt) =>
        ConsoleInput.Read(prompt, text => DomainValidation.RequireText(text, "Строка поиска"));

    /// <summary>Возвращает существующий вид конфет или null, если пользователь отменил ввод.</summary>
    private Candy? ReadCandyOrCancel(string prompt)
    {
        if (_factory.CandyCount == 0)
        {
            Console.WriteLine("Ассортимент пуст.");
            return null;
        }

        return ConsoleInput.Read<Candy?>($"{prompt} (Enter — отмена): ", text =>
            string.IsNullOrWhiteSpace(text) ? null : _factory.GetCandy(InputParser.ParseInt(text)));
    }

    private static CandyCategory ReadCategory()
    {
        var categories = PrintCategories();
        return categories[ConsoleInput.ReadInt("Номер категории: ", 1, categories.Length) - 1];
    }

    private static CandyCategory? ReadOptionalCategory(CandyCategory current)
    {
        var categories = PrintCategories();
        int? number = ConsoleInput.ReadOptional($"Номер категории [{current.ToDisplayName()}]: ", text =>
        {
            var value = InputParser.ParseInt(text);
            return value >= 1 && value <= categories.Length
                ? value
                : throw new FormatException($"Введите число от 1 до {categories.Length}.");
        });

        return number is int index ? categories[index - 1] : null;
    }

    private static CandyCategory[] PrintCategories()
    {
        CandyCategory[] categories = Enum.GetValues<CandyCategory>();
        for (var i = 0; i < categories.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {categories[i].ToDisplayName()}");
        }

        return categories;
    }

    private static Predicate<Candy> CreateCategoryFilter()
    {
        var category = ReadCategory();
        return candy => candy.Category == category;
    }

    private static Predicate<Candy> CreatePriceFilter()
    {
        var min = ConsoleInput.Read("Минимальная цена за кг, руб.: ", ParsePrice);
        var max = ConsoleInput.Read("Максимальная цена за кг, руб.: ", text =>
        {
            var value = ParsePrice(text);
            return value >= min ? value : throw new BusinessRuleException("Максимальная цена не может быть меньше минимальной.");
        });

        return candy => candy.PricePerKg >= min && candy.PricePerKg <= max;
    }

    private static SalesPeriod ReadPeriod()
    {
        var from = ConsoleInput.ReadDate("С даты (ДД.ММ.ГГГГ): ");
        return ConsoleInput.Read("По дату (ДД.ММ.ГГГГ): ", text => new SalesPeriod(from, InputParser.ParseDate(text)));
    }

    /// <summary>Пункт меню: ключ, заголовок и действие (делегат Action).</summary>
    private sealed record MenuItem(string Key, string Title, Action Execute);
}
