namespace ApisConsulta.Application.Payments.Exceptions;

/// <summary>
/// A reference sent with the payment does not exist in BambooERP (customer, bank,
/// payment form, user, department or sale), so the row cannot be inserted.
/// </summary>
public class PaymentValidationException : Exception
{
    public PaymentValidationException(string message) : base(message) { }
}
