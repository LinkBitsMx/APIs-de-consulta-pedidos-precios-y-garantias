using System.ComponentModel.DataAnnotations;

namespace ApisConsulta.Application.PreOrdenes.Requests;

public class CrearPreOrdenRequest
{
    [Required(ErrorMessage = "customerCode is required.")]
    [MaxLength(200)]
    public string CustomerCode { get; set; } = string.Empty;

    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "email format is invalid.")]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? Notes { get; set; }

    [Required(ErrorMessage = "items is required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CrearPreOrdenItemRequest> Items { get; set; } = [];
}

public class CrearPreOrdenItemRequest
{
    [Required(ErrorMessage = "productCode is required.")]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "quantity must be greater than zero.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "unitPrice cannot be negative.")]
    public decimal UnitPrice { get; set; }
}
