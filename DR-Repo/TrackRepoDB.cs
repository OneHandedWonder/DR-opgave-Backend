using Microsoft.EntityFrameworkCore;
using DR.Data;

namespace RecordsRepo;

public class TrackRepoDB
{
    private readonly RecordDbContext _db;

    public TrackRepoDB(RecordDbContext db)
    {
        _db = db;
    }

    public Track Add(Track track)
    {
        _db.Tracks.Add(track);
        _db.SaveChanges();
        return track;
    }

    public IEnumerable<Track> GetAll()
    {
        return _db.Tracks.AsNoTracking().ToList();
    }
}