using DR_Repo.Services;
using Microsoft.AspNetCore.Mvc;

namespace DR_Repo.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthStatusService _healthStatusService;

    public HealthController(IHealthStatusService healthStatusService)
    {
        _healthStatusService = healthStatusService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthStatusResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthStatusResponseDto>> Get(CancellationToken cancellationToken)
    {
        var payload = await _healthStatusService.GetStatusAsync(cancellationToken);

        if (string.Equals(payload.Status, "Unhealthy", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
        }

        return Ok(payload);
    }

    [HttpGet("tables")]
    [ProducesResponseType(typeof(Dictionary<string, TableStatusItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, TableStatusItemDto>>> GetTables(CancellationToken cancellationToken)
    {
        var payload = await _healthStatusService.GetStatusAsync(cancellationToken);
        return Ok(payload.Tables);
    }
}
