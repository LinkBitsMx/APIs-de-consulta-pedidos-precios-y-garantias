using ApisConsulta.Application.Consultas.Sales.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

/// <summary>
/// Sales queries. English-facing API (reviewed by the China team): routes, fields and
/// status values are in English.
/// </summary>
[Authorize]
[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Paged list of sales. Every sale carries its overall status and, once the
    /// quotation has been validated, the status of each warehouse order.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSales([FromQuery] SalesFilter filter)
    {
        var result = await _mediator.Send(new GetSalesQuery { Filter = filter });
        return Ok(result);
    }

    /// <summary>
    /// Full detail of a sale: customer, totals, invoicing, per-warehouse orders with
    /// their status and every line of the order.
    /// </summary>
    [HttpGet("{folio}")]
    public async Task<IActionResult> GetSaleByFolio(string folio)
    {
        var result = await _mediator.Send(new GetSaleByFolioQuery { Folio = folio });

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
