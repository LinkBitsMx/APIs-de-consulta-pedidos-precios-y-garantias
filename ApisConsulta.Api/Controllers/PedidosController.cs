using ApisConsulta.Application.Consultas.Pedidos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PedidosController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{folio}")]
    public async Task<IActionResult> GetPedidoByFolio(string folio)
    {
        var query = new GetPedidoByFolioQuery { Folio = folio };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{folio}/estatus")]
    public async Task<IActionResult> GetEstatusByFolio(string folio)
    {
        var query = new GetEstatusByFolioQuery { Folio = folio };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
