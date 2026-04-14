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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<IEnumerable<Record>> GetAll([FromQuery] string? search = null)
    {
        var records = recordRepository.GetAll(search);
        return Ok(records);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<Record> Create([FromBody] Record record)
    {
        if (record == null)
            return BadRequest("Record body is required.");

        var createdRecord = recordRepository.Add(record);
        return CreatedAtAction(nameof(GetAll), new { id = createdRecord.Id }, createdRecord);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<Record> Delete([FromRoute] int id)
    {
        var record = recordRepository.GetById(id);
        if (record == null)
        {
            return NotFound($"Record with id {id} not found.");
        }

        recordRepository.Delete(id);
        return Ok(record);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<Record> Update([FromRoute] int id, [FromBody] Record record)
    {
        if (record == null)
            return BadRequest("Record body is required.");

        record.Id = id;
        var updatedRecord = recordRepository.Update(id, record);
        if (updatedRecord == null)
        {
            return NotFound($"Record with id {id} not found.");
        }

        return Ok(updatedRecord);
    }
}