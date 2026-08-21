using ApisConsulta.Application.Payments.Commands;
using ApisConsulta.Application.Payments.Exceptions;
using ApisConsulta.Application.Payments.Queries;
using ApisConsulta.Application.Payments.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

/// <summary>
/// Payment registration. English-facing API (reviewed by the China team): routes, fields
/// and status values are in English.
/// </summary>
[Authorize]
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Registers a payment in BambooERP exactly as the ERP does today: the row is created
    /// pending validation (status 4) and the database generates its folio. When
    /// <c>saleFolio</c> is sent the payment is related to that sale.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var command = new CreatePaymentCommand { Data = request };

        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.PaymentId }, result);
        }
        catch (PaymentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Paged list of payments, filtered by status in English: <c>VALID</c>,
    /// <c>REJECTED</c>, <c>PENDING</c>, <c>IN_PROCESS</c> or <c>CANCELLED</c>, several of
    /// them comma separated (<c>status=REJECTED,IN_PROCESS</c>). Every payment carries the
    /// same full record the detail endpoint returns, and <c>summary</c> adds how many —
    /// and how much — each status adds up to across the whole filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentsFilter filter)
    {
        try
        {
            var result = await _mediator.Send(new GetPaymentsQuery { Filter = filter });
            return Ok(result);
        }
        catch (PaymentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Payment as it is stored, with its bank account, payment form and status.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetPaymentByIdQuery { PaymentId = id });

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
