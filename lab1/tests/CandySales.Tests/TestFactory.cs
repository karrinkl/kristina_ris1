using System;
using CandySales.Domain;

namespace CandySales.Tests;

/// <summary>Фабрика с фиксированными «часами» и небольшим ассортиментом для тестов.</summary>
internal static class TestFactory
{
    public static readonly DateTime Now = new(2025, 3, 15, 12, 0, 0);

    public static ConfectioneryFactory CreateEmpty() => new("Тестовая фабрика", () => Now);

    public static ConfectioneryFactory CreateWithAssortment()
    {
        var factory = CreateEmpty();
        factory.AddCandy("Лесная сказка", CandyCategory.Chocolate, 845.00m, 100m);
        factory.AddCandy("Барбарисовая карамель", CandyCategory.Caramel, 410.50m, 200m);
        factory.AddCandy("Мятная карамель", CandyCategory.Caramel, 395.00m, 15m);
        factory.AddCandy("Помадка крем-брюле", CandyCategory.Fondant, 480.40m, 50m);
        return factory;
    }
}
