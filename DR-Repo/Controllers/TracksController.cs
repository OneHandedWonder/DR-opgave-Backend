using Microsoft.AspNetCore.Mvc;
using DR.Data;
using RecordsRepo;

namespace DR_Repo.Controllers;

[ApiController]
[Route("api/v2/tracks")]
public class TracksController : ControllerBase
{
    private readonly TrackRepoDB trackRepository;

    public TracksController(TrackRepoDB trackRepository)
    {
        this.trackRepository = trackRepository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Track>> GetAll()
    {
        return Ok(trackRepository.GetAll());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Track> Create([FromBody] Track track)
    {
        if (track == null)
        {
            return BadRequest("Track body is required.");
        }

        var createdTrack = trackRepository.Add(track);
        return CreatedAtAction(nameof(GetAll), new { id = createdTrack.Id }, createdTrack);
    }
}