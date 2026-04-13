using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using DR.Data;
using RecordsRepo;
[ApiController]
[Route("api/v2/records")]
public class RecordsController : ControllerBase
{
    private static readonly RecordRepoDB recordRepository = new RecordRepoDB(new RecordDbContext());

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Record>> GetAll()
    {
        var records = recordRepository.GetAll();
        return Ok(records);
    
    }

}