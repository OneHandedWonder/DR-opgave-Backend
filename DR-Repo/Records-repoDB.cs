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

	public IEnumerable<Record> GetAll(string? search = null)
	{
		var query = _db.Records.AsNoTracking().AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = $"%{search.Trim()}%";
			query = query.Where(r =>
				EF.Functions.ILike(r.Name, term) ||
				EF.Functions.ILike(r.Artist, term) ||
				EF.Functions.ILike(r.Genre, term));
		}

		return query.ToList();
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
