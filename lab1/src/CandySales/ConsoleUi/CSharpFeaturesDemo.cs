// Nullable reference types включены для всего проекта (<Nullable>enable</Nullable> в Directory.Build.props).
// Директива ниже показывает, как управлять этим режимом на уровне отдельного файла.
#nullable enable

using System;
using System.IO;
using CandySales.Domain;

namespace CandySales.ConsoleUi;

/// <summary>
/// Изолированный учебный пример (пункт меню 14): различия типов-значений и ссылочных типов,
/// boxing/unboxing, nullable-типы и pattern matching. В предметной логике упаковка не используется,
/// поэтому она вынесена сюда, чтобы не ухудшать основной код.
/// </summary>
internal static class CSharpFeaturesDemo
{
    public static void Run(TextWriter output)
    {
        ShowValueAndReferenceTypes(output);
        ShowBoxing(output);
        ShowNullableTypes(output);
        ShowPatternMatching(output);
    }

    private static void ShowValueAndReferenceTypes(TextWriter output)
    {
        output.WriteLine("1. Типы-значения (struct) и ссылочные типы (class)");

        var structA = new PackageStruct { WeightKg = 1m };
        var structB = structA; // копируется всё значение
        structB.WeightKg = 5m;
        output.WriteLine($"   struct: a = {structA.WeightKg} кг, b = {structB.WeightKg} кг — b получила копию, a не изменилась");

        var classA = new PackageClass { WeightKg = 1m };
        var classB = classA; // копируется только ссылка на объект в куче
        classB.WeightKg = 5m;
        output.WriteLine($"   class:  a = {classA.WeightKg} кг, b = {classB.WeightKg} кг — обе переменные указывают на один объект");
        output.WriteLine($"   ReferenceEquals(classA, classB) = {ReferenceEquals(classA, classB)}");
    }

    private static void ShowBoxing(TextWriter output)
    {
        output.WriteLine("2. Boxing и unboxing");

        int boxesInStock = 42;
        object boxed = boxesInStock; // boxing: копия значения помещается в объект в управляемой куче
        boxesInStock = 50;
        int unboxed = (int)boxed; // unboxing: явное приведение к точному исходному типу
        output.WriteLine($"   переменная = {boxesInStock}, упакованная копия = {unboxed}, тип объекта: {boxed.GetType().Name}");

        try
        {
            output.WriteLine((long)boxed);
        }
        catch (InvalidCastException)
        {
            output.WriteLine("   (long)boxed -> InvalidCastException: распаковать можно только в исходный тип int");
        }

        output.WriteLine("   Обобщённые коллекции (List<decimal>, Dictionary<int, Candy>) хранят значения без упаковки.");
    }

    private static void ShowNullableTypes(TextWriter output)
    {
        output.WriteLine("3. Nullable value types и nullable reference types");

        int? shelfLifeDays = null; // Nullable<int>: значение может отсутствовать
        output.WriteLine($"   int? shelfLifeDays = null: HasValue = {shelfLifeDays.HasValue}, shelfLifeDays ?? 180 = {shelfLifeDays ?? 180}");
        shelfLifeDays = 90;
        output.WriteLine($"   после присваивания 90: HasValue = {shelfLifeDays.HasValue}, Value = {shelfLifeDays.Value}");

        string? customer = null; // аннотация «может быть null»; проверку выполняет компилятор
        output.WriteLine($"   string? customer = null: customer?.Length ?? 0 = {customer?.Length ?? 0}");
        customer = "Магазин «Ласточка»";
        output.WriteLine($"   после присваивания компилятор знает, что значение не null: Length = {customer.Length}");
    }

    private static void ShowPatternMatching(TextWriter output)
    {
        output.WriteLine("4. Pattern matching и switch-выражение");

        // Элементы массива object?[] — ссылки, поэтому int и decimal здесь упаковываются.
        object?[] values = [42, 12.5m, "Карамель", string.Empty, CandyCategory.Toffee, null, new SalesPeriod(new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 31))];
        foreach (var value in values)
        {
            output.WriteLine($"   {Describe(value)}");
        }
    }

    private static string Describe(object? value) => value switch
    {
        null => "null — значение отсутствует",
        int number and > 0 => $"положительное целое число {number}",
        decimal amount => $"decimal {amount} — подходит для денежных сумм и масс",
        string { Length: 0 } => "пустая строка",
        string text => $"строка «{text}» длиной {text.Length}",
        CandyCategory category => $"категория конфет «{category.ToDisplayName()}»",
        SalesPeriod period => $"отчётный период {period} (struct)",
        _ => $"значение типа {value.GetType().Name}",
    };

    private struct PackageStruct
    {
        public decimal WeightKg { get; set; }
    }

    private sealed class PackageClass
    {
        public decimal WeightKg { get; set; }
    }
}
