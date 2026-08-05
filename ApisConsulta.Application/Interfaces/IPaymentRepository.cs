using ApisConsulta.Application.Payments.Requests;
using ApisConsulta.Application.Payments.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IPaymentRepository
{
    /// <summary>
    /// Resolves in a single round trip every reference the payment points to, so the
    /// request can be rejected before touching <c>Payments</c>.
    /// </summary>
    Task<PaymentReferences> ResolveReferencesAsync(CreatePaymentRequest request);

    Task<PaymentResponse> CreateAsync(CreatePaymentData data);

    Task<PaymentResponse?> GetByIdAsync(int paymentId);
}

/// <summary>References of a payment as they exist (or not) in BambooERP.</summary>
public class PaymentReferences
{
    public int? CustomerId { get; set; }

    /// <summary>Receiving company of the bank account (<c>bancos.id_origen</c>).</summary>
    public int? AccountId { get; set; }

    public bool BankExists { get; set; }
    public bool BankDisabled { get; set; }
    public bool PaymentFormExists { get; set; }
    public bool UploaderExists { get; set; }

    /// <summary>Branch of the uploading user (<c>catUsers.BranchID</c>), used as default department.</summary>
    public int? UploaderDepartmentId { get; set; }

    public bool SellerExists { get; set; }
    public bool DepartmentExists { get; set; }
    public bool StatusExists { get; set; }

    /// <summary>Sale matching <c>saleFolio</c> (<c>quotation.id</c>), when one was sent.</summary>
    public int? SaleId { get; set; }
}

/// <summary>Payment ready to be inserted: request values with every reference resolved.</summary>
public class CreatePaymentData
{
    public int CustomerId { get; set; }
    public DateTime PaymentDate { get; set; }
    public int AccountId { get; set; }
    public int BankId { get; set; }
    public int PaymentFormId { get; set; }
    public int StatusId { get; set; }
    public decimal? Amount { get; set; }
    public string PaymentType { get; set; } = "payment";
    public string? Reference { get; set; }
    public string? Comentary { get; set; }
    public string? Observations { get; set; }
    public string? PaymentFilePath { get; set; }

    /// <summary>Sale the payment is applied to; 0 when the payment is not related to one.</summary>
    public int SaleId { get; set; }

    public int UploadedById { get; set; }
    public int? SellerId { get; set; }
    public int DepartmentId { get; set; }
}
