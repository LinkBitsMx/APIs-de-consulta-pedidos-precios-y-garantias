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
            DepartmentId = departmentId.Value
        });
    }

    private static string? Trim(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
