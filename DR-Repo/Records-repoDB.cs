using Microsoft.EntityFrameworkCore;
using DR.Data;

namespace RecordsRepo;

public class RecordRepoDB
{
	private readonly RecordDbContext _db;

	public RecordRepoDB(RecordDbContext db)
	{
		_db = db;
	}

	public IEnumerable<Record> GetAll()
	{
		return _db.Records.AsNoTracking().ToList();
	}

	public Record Add(Record record)
	{
		_db.Records.Add(record);
		_db.SaveChanges();
		return record;
	}

	public Record? GetById(int id)
	{
		return _db.Records.FirstOrDefault(m => m.Id == id);
	}

	public Record? Delete(int id)
	{
		var record = GetById(id);
		if (record != null)
		{
			_db.Records.Remove(record);
			_db.SaveChanges();
		}
		return record;
	}

	public Record? Update(int id, Record updatedRecord)
	{
		var record = GetById(id);
		if (record == null)
		{
			return null;
		}

		record.Name = updatedRecord.Name;
		record.ReleaseYear = updatedRecord.ReleaseYear;
		record.Genre = updatedRecord.Genre;
		record.Artist = updatedRecord.Artist;
		record.trackCount = updatedRecord.trackCount;
		record.Duration = updatedRecord.Duration;
		_db.SaveChanges();
		return record;
	}
}
