using System;
using CandySales.Domain;

namespace CandySales.Infrastructure;

/// <summary>
/// Начальный ассортимент и история продаж за последнюю неделю, чтобы приложение
/// можно было демонстрировать сразу после запуска. Данные вымышленные.
/// </summary>
public static class DemoData
{
    public const string FactoryName = "Кондитерская фабрика «Сладкий край»";

    public static void Seed(ConfectioneryFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var forestTale = factory.AddCandy("Лесная сказка", CandyCategory.Chocolate, 845.00m, 120m);
        var truffle = factory.AddCandy("Трюфель классический", CandyCategory.Chocolate, 1120.00m, 64.5m);
        var barberry = factory.AddCandy("Барбарисовая карамель", CandyCategory.Caramel, 410.50m, 210m);
        var mint = factory.AddCandy("Мятная карамель", CandyCategory.Caramel, 395.00m, 26m);
        var toffee = factory.AddCandy("Сливочная ириска", CandyCategory.Toffee, 520.00m, 95m);
        var cherry = factory.AddCandy("Вишнёвый мармелад", CandyCategory.Jelly, 460.00m, 140m);
        factory.AddCandy("Арахис в шоколадной глазури", CandyCategory.Dragee, 690.00m, 75m);
        var cremeBrulee = factory.AddCandy("Помадка крем-брюле", CandyCategory.Fondant, 480.40m, 50m);
        factory.AddCandy("Суфле ванильное", CandyCategory.Souffle, 760.00m, 0m);

        var today = factory.Now.Date;
        factory.SellCandy(barberry.Id, 40m, "ООО «Продторг»", today.AddDays(-6).AddHours(9.5));
        factory.SellCandy(forestTale.Id, 12.5m, "Магазин «Ласточка»", today.AddDays(-5).AddHours(11));
        factory.SellCandy(toffee.Id, 8m, null, today.AddDays(-5).AddHours(15.25));
        factory.SellCandy(cherry.Id, 25m, "ООО «Продторг»", today.AddDays(-4).AddHours(10));
        factory.SellCandy(truffle.Id, 4.5m, "Кафе «Шоколадница на углу»", today.AddDays(-3).AddHours(13));
        factory.SellCandy(barberry.Id, 30m, "Магазин «Ласточка»", today.AddDays(-2).AddHours(9));
        factory.SellCandy(cremeBrulee.Id, 2.5m, null, today.AddDays(-2).AddHours(16.5));
        factory.SellCandy(mint.Id, 9m, "ИП Орлова", today.AddDays(-1).AddHours(12));
        factory.SellCandy(forestTale.Id, 7.25m, null, today.AddDays(-1).AddHours(17));
    }
}
