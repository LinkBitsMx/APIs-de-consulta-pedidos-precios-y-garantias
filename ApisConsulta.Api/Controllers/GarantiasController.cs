using ApisConsulta.Application.Consultas.Garantias.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GarantiasController : ControllerBase
{
    private readonly IMediator _mediator;

    public GarantiasController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{folioTicket}")]
    public async Task<IActionResult> GetGarantiaByFolio(string folioTicket)
    {
        var query = new GetGarantiaByFolioQuery { FolioTicket = folioTicket };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
