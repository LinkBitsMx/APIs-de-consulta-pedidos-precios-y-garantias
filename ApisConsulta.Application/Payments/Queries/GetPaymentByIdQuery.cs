using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.Payments.Response;
using MediatR;

namespace ApisConsulta.Application.Payments.Queries;

public class GetPaymentByIdQuery : IRequest<PaymentResponse?>
{
    public int PaymentId { get; set; }
}

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, PaymentResponse?>
{
    private readonly IPaymentRepository _repository;

    public GetPaymentByIdQueryHandler(IPaymentRepository repository) => _repository = repository;

    public Task<PaymentResponse?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.PaymentId);
}
