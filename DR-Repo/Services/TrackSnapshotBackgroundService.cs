using DR.Data;
using RecordsRepo;
using System.Diagnostics.CodeAnalysis;

namespace DR_Repo.Services;

public sealed class TrackSnapshotBackgroundService : BackgroundService
{
    private static readonly string[] ChannelSlugs =
    [
        "p1",
        "p2",
        "p3",
        "p4kbh",
        "p5kbh",
        "p6beat"
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrackSnapshotBackgroundService> _logger;

    public TrackSnapshotBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TrackSnapshotBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Track snapshot background job started.");

        await CaptureAndPersistAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CaptureAndPersistAsync(stoppingToken);
        }
    }

    private async Task CaptureAndPersistAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var radioService = scope.ServiceProvider.GetRequiredService<DRRadioService>();
            var trackRepository = scope.ServiceProvider.GetRequiredService<TrackRepoDB>();

            var insertedCount = 0;

            foreach (var channelSlug in ChannelSlugs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CurrentTrackDto? current;
                try
                {
                    current = await radioService.GetCurrentTrackForChannelAsync(channelSlug);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed fetching current track for channel {ChannelSlug}.", channelSlug);
                    continue;
                }

                if (current is null)
                {
                    continue;
                }

                var currentTrack = current.CurrentTrack;
                if (!HasPlayableTrack(currentTrack))
                {
                    continue;
                }

                var (artist, title) = SplitTrack(currentTrack);
                if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var channel = current.ChannelSlug ?? channelSlug;
                var nowUtc = DateTime.UtcNow;

                // Hard duplicate guard: do not insert if this track already exists in DB.
                if (trackRepository.ExistsByIdentity(title, artist, channel))
                {
                    continue;
                }

                var latest = trackRepository.GetLatestByChannel(channel);
                var isSameAsLatest =
                    latest != null &&
                    string.Equals(latest.Name?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(latest.Artist?.Trim(), artist.Trim(), StringComparison.OrdinalIgnoreCase);

                // Only store a row when the channel changed to a new track.
                if (isSameAsLatest)
                {
                    continue;
                }

                trackRepository.Add(new Track
                {
                    Name = title,
                    Artist = artist,
                    Channel = channel,
                    PlayedAt = nowUtc
                });

                insertedCount++;
            }

            _logger.LogInformation("Track snapshot background job completed. Inserted {InsertedCount} track rows.", insertedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while persisting track snapshots.");
        }
    }

    private static (string artist, string title) SplitTrack(string currentTrack)
    {
        var parts = currentTrack.Split(" - ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            var artist = parts[0].TrimStart('/', ' ').Trim();
            var title = parts[1].Trim();
            return (artist, title);
        }

        return ("Unknown Artist", currentTrack.Trim());
    }

    private static bool HasPlayableTrack([NotNullWhen(true)] string? currentTrack)
    {
        if (string.IsNullOrWhiteSpace(currentTrack))
        {
            return false;
        }

        return !string.Equals(currentTrack.Trim(), "null", StringComparison.OrdinalIgnoreCase);
    }
}