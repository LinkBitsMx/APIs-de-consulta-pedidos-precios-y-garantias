using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.Payments.Exceptions;
using ApisConsulta.Application.Payments.Requests;
using ApisConsulta.Application.Payments.Response;
using MediatR;

namespace ApisConsulta.Application.Payments.Commands;

public class CreatePaymentCommand : IRequest<PaymentResponse>
{
    public CreatePaymentRequest Data { get; set; } = new();
}

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    /// <summary>PENDIENTE (<c>catEstatus</c>): the status the ERP creates payments with.</summary>
    private const int StatusPending = 4;

    private static readonly string[] PaymentTypes = ["payment", "credit", "advance"];

    private readonly IPaymentRepository _repository;

    public CreatePaymentCommandHandler(IPaymentRepository repository) => _repository = repository;

    public async Task<PaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;

        var paymentType = string.IsNullOrWhiteSpace(data.PaymentType)
            ? "payment"
            : data.PaymentType.Trim().ToLower();

        if (!PaymentTypes.Contains(paymentType))
            throw new PaymentValidationException(
                $"paymentType must be one of: {string.Join(", ", PaymentTypes)}.");

        var references = await _repository.ResolveReferencesAsync(data);

        if (references.CustomerId == null)
            throw new PaymentValidationException($"Customer '{data.CustomerCode}' not found.");

        if (!references.BankExists || references.AccountId == null)
            throw new PaymentValidationException($"Bank account {data.BankId} not found.");

        if (references.BankDisabled)
            throw new PaymentValidationException($"Bank account {data.BankId} is disabled.");

        if (!references.PaymentFormExists)
            throw new PaymentValidationException($"Payment form {data.PaymentFormId} not found.");

        if (!references.UploaderExists)
            throw new PaymentValidationException($"User {data.UploadedById} not found.");

        if (data.SellerId.HasValue && !references.SellerExists)
            throw new PaymentValidationException($"Seller {data.SellerId} not found.");

        if (data.DepartmentId.HasValue && !references.DepartmentExists)
            throw new PaymentValidationException($"Department {data.DepartmentId} not found.");

        if (data.StatusId.HasValue && !references.StatusExists)
            throw new PaymentValidationException($"Status {data.StatusId} not found.");

        if (!string.IsNullOrWhiteSpace(data.SaleFolio) && references.SaleId == null)
            throw new PaymentValidationException($"Sale '{data.SaleFolio}' not found.");

        // The department is required by the table; when it is not sent it falls back to the
        // branch of the user registering the payment, which is what the ERP records.
        var departmentId = data.DepartmentId ?? references.UploaderDepartmentId;
        if (departmentId == null || departmentId == 0)
            throw new PaymentValidationException(
                "departmentId is required: the user has no branch to take it from.");

        // Kingdee currencies: the receipt currency defaults to the settlement one, and the
        // rate defaults to 1 while both match. When they differ the rate has to come in the
        // request — BambooERP has no exchange rate table to take it from.
        var receiptCurrencySent = !string.IsNullOrWhiteSpace(data.ReceiveCurrencyCode);
        var settleCurrency = Upper(data.SettleCurrencyCode);
        var receiveCurrency = receiptCurrencySent ? Upper(data.ReceiveCurrencyCode) : settleCurrency;
        var receiveCurrencyId = data.ReceiveCurrencyId ?? (receiptCurrencySent ? null : data.SettleCurrencyId);
        var sameCurrency = string.Equals(settleCurrency, receiveCurrency, StringComparison.Ordinal);

        if (!sameCurrency && data.ExchangeRate == null)
            throw new PaymentValidationException(
                "exchangeRate is required when settleCurrencyCode and receiveCurrencyCode differ.");

        var exchangeRate = data.ExchangeRate ?? (sameCurrency ? 1m : null);

        return await _repository.CreateAsync(new CreatePaymentData
        {
            CustomerId = references.CustomerId.Value,
            PaymentDate = (data.PaymentDate ?? DateTime.Today).Date,
            AccountId = references.AccountId.Value,
            BankId = data.BankId,
            PaymentFormId = data.PaymentFormId,
            StatusId = data.StatusId ?? StatusPending,
            Amount = data.Amount,
            PaymentType = paymentType,
            Reference = Trim(data.Reference),
            Comentary = Trim(data.Comentary),
            Observations = Trim(data.Observations),
            PaymentFilePath = Trim(data.PaymentFilePath),
            SaleId = references.SaleId ?? 0,
            UploadedById = data.UploadedById,
            SellerId = data.SellerId,
            DepartmentId = departmentId.Value,

            KingdeeBillNo = Trim(data.KingdeeBillNo),
            BizOrgId = data.BizOrgId,
            BizOrgCode = Trim(data.BizOrgCode),
            SettleOrgId = data.SettleOrgId,
            SettleOrgCode = Trim(data.SettleOrgCode),
            CashierId = data.CashierId,
            CashierCode = Trim(data.CashierCode),
            KingdeeAccountId = data.KingdeeAccountId,
            KingdeeAccountCode = Trim(data.KingdeeAccountCode),
            ReceiveTypeId = data.ReceiveTypeId,
            ReceiveTypeCode = Trim(data.ReceiveTypeCode),
            SettleCurrencyId = data.SettleCurrencyId,
            SettleCurrencyCode = settleCurrency,
            ReceiveCurrencyId = receiveCurrencyId,
            ReceiveCurrencyCode = receiveCurrency,
            ExchangeRate = exchangeRate,
            CardId = data.CardId,
            CardNumber = Trim(data.CardNumber),
            MemberId = data.MemberId,
            MemberCardNumber = Trim(data.MemberCardNumber),
            RechargeAmount = data.RechargeAmount
        });
    }

    private static string? Trim(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Upper(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
