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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<Record> Create([FromBody] Record record)
    {
        var createdRecord = recordRepository.Create(record);
        return CreatedAtAction(nameof(GetAll), new { id = createdRecord.Id }, createdRecord);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Record> Delete([FromRoute] int id)
    {
        var record = recordRepository.GetById(id);
        if (record == null)
        {
            return NotFound();
        }

        recordRepository.Delete(id);
        return Ok(record);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Record> Update([FromRoute] int id, [FromBody] Record record)
    {
        var existingRecord = recordRepository.GetById(id);
        if (existingRecord == null)
        {
            return NotFound();
        }

        record.Id = id;
        recordRepository.Update(record);
        return Ok(record);
    }
}