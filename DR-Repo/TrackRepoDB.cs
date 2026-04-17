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

    public bool ExistsByIdentity(string name, string artist, string channel)
    {
        var normalizedName = name.Trim();
        var normalizedArtist = artist.Trim();
        var normalizedChannel = channel.Trim();

        return _db.Tracks
            .AsNoTracking()
            .Any(t =>
                t.Channel.ToLower() == normalizedChannel.ToLower() &&
                t.Name.ToLower() == normalizedName.ToLower() &&
                t.Artist.ToLower() == normalizedArtist.ToLower());
    }

    public Track? GetLatestByChannel(string channel)
    {
        return _db.Tracks
            .AsNoTracking()
            .Where(t => t.Channel == channel)
            .OrderByDescending(t => t.PlayedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefault();
    }

    public IEnumerable<Track> GetAll()
    {
        return _db.Tracks.AsNoTracking().ToList();
    }
}