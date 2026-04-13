using DR.Data;

namespace RecordsRepo;
public class RecordRepository
{
    private readonly List<Record> records = new List<Record>();

    public IEnumerable<Record> GetAll()
    {
        return records.AsReadOnly();
    }

    public Record Add(Record record)
    {
        record.Id = records.Count > 0 ? records.Max(m => m.Id) + 1 : 1;
        records.Add(record);
        return record;
    }

    public Record? GetById(int id)
    {
        return records.FirstOrDefault(m => m.Id == id);
    }

    public Record? Delete(int id)
    {
        var record = GetById(id);
        if (record != null)
        {
            records.Remove(record);
        }
        return record;
    }

    public Record? Update(int id, Record updatedRecord)
    {
        var record = GetById(id);
        if (record != null)
        {
            record.Name = updatedRecord.Name;
            record.ReleaseYear = updatedRecord.ReleaseYear;
            record.Genre = updatedRecord.Genre;
            record.Artist = updatedRecord.Artist;
            record.trackCount = updatedRecord.trackCount;
            record.Duration = updatedRecord.Duration;
        }
        else
        {
            return null;
        }
        return record;
    }
}