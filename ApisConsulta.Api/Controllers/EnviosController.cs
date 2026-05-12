using ApisConsulta.Application.Consultas.Envios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EnviosController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnviosController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{folio}")]
    public async Task<IActionResult> GetEnvioByFolio(string folio)
    {
        var query = new GetEnvioByFolioQuery { Folio = folio };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
