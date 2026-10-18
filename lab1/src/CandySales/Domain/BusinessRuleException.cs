using System;

namespace CandySales.Domain;

/// <summary>
/// Нарушение правил предметной области: некорректные данные о конфетах или продаже
/// либо недопустимая операция (например, недостаточный остаток на складе).
/// Сообщение исключения предназначено для показа пользователю.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
