using System;

namespace CandySales.Domain;

/// <summary>Категория (вид) конфет, выпускаемых фабрикой.</summary>
public enum CandyCategory
{
    Chocolate,
    Caramel,
    Toffee,
    Jelly,
    Dragee,
    Fondant,
    Souffle,
}

/// <summary>Русские названия категорий для вывода пользователю.</summary>
public static class CandyCategoryExtensions
{
    public static string ToDisplayName(this CandyCategory category) => category switch
    {
        CandyCategory.Chocolate => "Шоколадные",
        CandyCategory.Caramel => "Карамель",
        CandyCategory.Toffee => "Ириски",
        CandyCategory.Jelly => "Мармелад",
        CandyCategory.Dragee => "Драже",
        CandyCategory.Fondant => "Помадные",
        CandyCategory.Souffle => "Суфле",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Неизвестная категория конфет."),
    };
}
