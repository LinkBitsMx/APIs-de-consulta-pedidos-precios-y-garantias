namespace ApisConsulta.Application.PreOrdenes.Exceptions;

/// <summary>
/// Se lanza cuando el customer_code recibido en la pre-orden no existe en la tabla customers.
/// </summary>
public class CustomerNoEncontradoException : Exception
{
    public string CustomerCode { get; }

    public CustomerNoEncontradoException(string customerCode)
        : base($"El customer_code '{customerCode}' no existe en la tabla de clientes.")
    {
        CustomerCode = customerCode;
    }
}
