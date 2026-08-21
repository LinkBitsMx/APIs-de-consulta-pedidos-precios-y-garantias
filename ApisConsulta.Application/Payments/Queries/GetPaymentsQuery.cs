using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.Payments.Exceptions;
using ApisConsulta.Application.Payments.Response;
using MediatR;

namespace ApisConsulta.Application.Payments.Queries;

public class GetPaymentsQuery : IRequest<PagedPaymentsResponse>
{
    public PaymentsFilter Filter { get; set; } = new();
}

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, PagedPaymentsResponse>
{
    private readonly IPaymentRepository _repository;

    public GetPaymentsQueryHandler(IPaymentRepository repository) => _repository = repository;

    public Task<PagedPaymentsResponse> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter.Normalized();

        // A status the API does not publish is rejected instead of silently returning an
        // empty page: a typo in the filter would otherwise read as "there are none".
        var unknown = filter.StatusValues()
            .Where(s => !PaymentsFilter.Statuses.Contains(s))
            .ToList();

        if (unknown.Count > 0)
            throw new PaymentValidationException(
                $"Unknown status: {string.Join(", ", unknown)}. " +
                $"status must be one of: {string.Join(", ", PaymentsFilter.Statuses)}.");

        return _repository.GetPaymentsAsync(filter);
    }
}
