using ApisConsulta.Application.PreOrdenes.Commands;
using ApisConsulta.Application.PreOrdenes.Exceptions;
using ApisConsulta.Application.PreOrdenes.Queries;
using ApisConsulta.Application.PreOrdenes.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PreOrdenesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreOrdenesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Crea una pre-orden (sin confirmar) enviada desde el sistema externo de clientes.
    /// Queda en estatus PENDIENTE para que el vendedor la tome y la convierta en cotización.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPreOrdenRequest request)
    {
        var command = new CrearPreOrdenCommand { Datos = request };

        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (CustomerNoEncontradoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Lista las pre-órdenes, opcionalmente filtradas por estatus.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var query = new GetPreOrdenesQuery { Status = status };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Lists every pre-order with the full per-item detail: stock per warehouse,
    /// covered/shortage quantity, fulfillment status and suggested allocation.
    /// English-facing endpoint (reviewed by the China team); absolute route so it is
    /// served at /api/preorders/detail while the rest stays under /api/preordenes.
    /// </summary>
    [HttpGet("/api/preorders/detail")]
    public async Task<IActionResult> GetAllDetail([FromQuery] string? status)
    {
        var query = new GetPreOrdersDetailQuery { Status = status };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>Devuelve métricas resumidas por pre-orden: cliente, lo solicitado y lo confirmado.</summary>
    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas([FromQuery] string? status)
    {
        var query = new GetPreOrdenMetricasQuery { Status = status };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>Obtiene el detalle de una pre-orden con sus items.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetPreOrdenByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
