using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using DR.Data;
using RecordsRepo;
[ApiController]
[Route("api/v2/records")]
public class RecordsControllerv2 : ControllerBase
{
    private readonly RecordRepoDB recordRepository;

    public RecordsControllerv2(RecordRepoDB recordRepository)
    {
        this.recordRepository = recordRepository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Record>> GetAll([FromQuery] string? search = null)
    {
        var records = recordRepository.GetAll(search);
        return Ok(records);
    }

}