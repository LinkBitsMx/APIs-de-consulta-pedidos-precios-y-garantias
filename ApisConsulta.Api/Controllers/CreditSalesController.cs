using ApisConsulta.Application.CreditSales.Exceptions;
using ApisConsulta.Application.CreditSales.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

/// <summary>
/// Credit sales queries. English-facing API (reviewed by the China team): routes, fields
/// and status values are in English.
/// </summary>
[Authorize]
[ApiController]
[Route("api/credit-sales")]
public class CreditSalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CreditSalesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Paged list of the sales that went on credit, each one with the payments applied
    /// against it. Filtered by status in English: <c>PAID</c>, <c>OVERDUE</c> or
    /// <c>PENDING</c>, several of them comma separated
    /// (<c>status=OVERDUE,PENDING</c> for everything still owed).
    ///
    /// <c>summary</c> answers how many credit sales there are and how long the customer
    /// takes to pay them — over the whole filter, not just this page — and
    /// <c>byCustomer</c> breaks the same figures down per customer.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCreditSales([FromQuery] CreditSalesFilter filter)
    {
        try
        {
            var result = await _mediator.Send(new GetCreditSalesQuery { Filter = filter });
            return Ok(result);
        }
        catch (CreditSaleValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
