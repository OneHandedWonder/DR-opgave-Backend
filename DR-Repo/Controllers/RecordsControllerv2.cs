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
    public ActionResult<IEnumerable<Record>> GetAll()
    {
        var records = recordRepository.GetAll();
        return Ok(records);
    }

}