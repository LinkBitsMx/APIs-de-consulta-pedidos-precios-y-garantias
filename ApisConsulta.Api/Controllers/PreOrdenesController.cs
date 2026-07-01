using ApisConsulta.Application.PreOrdenes.Commands;
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
        var result = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Lista las pre-órdenes, opcionalmente filtradas por estatus.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var query = new GetPreOrdenesQuery { Status = status };
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
