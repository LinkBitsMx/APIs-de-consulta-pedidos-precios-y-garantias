using ApisConsulta.Application.Consultas.Precios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PreciosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreciosController(IMediator mediator) => _mediator = mediator;

    [HttpGet("productos/{identificador}")]
    public async Task<IActionResult> GetPrecioByIdentificador(string identificador)
    {
        var query = new GetPrecioByIdentificadorQuery { Identificador = identificador };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
