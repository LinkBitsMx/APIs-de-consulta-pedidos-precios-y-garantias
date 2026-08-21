namespace ApisConsulta.Application.CreditSales.Exceptions;

/// <summary>
/// The request cannot be served as it came: it is answered with 400 and the message,
/// never with an empty page that would read as "there are no credit sales".
/// </summary>
public class CreditSaleValidationException : Exception
{
    public CreditSaleValidationException(string message) : base(message) { }
}
