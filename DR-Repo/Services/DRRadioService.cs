using System.Text.Json;
using System.Text.RegularExpressions;

namespace DR_Repo.Services
{
    public class DRRadioService
    {
        private readonly HttpClient _httpClient;
        private const string DR_API_BASE = "https://api.dr.dk/radio/v5";

        public DRRadioService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ChannelNowPlayingDto>> GetNowPlayingAllChannelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{DR_API_BASE}/schedules/all/now");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"DR API returned {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var episodes = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();

                var channelGrouped = episodes
                    .Where(e => e.TryGetProperty("channel", out _)) // Filter out items without channel
                    .GroupBy(e => SafeGetString(e, "channel", "slug"))
                    .Where(g => g.Key != null)
                    .Select(g => new ChannelNowPlayingDto
                    {
                        ChannelSlug = g.Key,
                        ChannelTitle = SafeGetString(g.First(), "channel", "title"),
                        NowPlaying = new NowPlayingDto
                        {
                            Title = SafeGetString(g.First(), "title"),
                            Description = SafeGetString(g.First(), "description"),
                            StartTime = SafeGetDateTime(g.First(), "startTime"),
                            EndTime = SafeGetDateTime(g.First(), "endTime"),
                            Categories = SafeGetStringArray(g.First(), "categories"),
                            SeriesTitle = SafeGetString(g.First(), "series", "title"),
                            PresentationUrl = SafeGetString(g.First(), "presentationUrl"),
                            IsAvailableOnDemand = SafeGetBoolean(g.First(), "isAvailableOnDemand")
                        }
                    })
                    .OrderBy(c => c.ChannelSlug)
                    .ToList();

                return channelGrouped;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error fetching DR radio data: {ex.Message}", ex);
            }
        }

        private string? SafeGetString(JsonElement element, params string[] propertyPath)
        {
            JsonElement current = element;
            foreach (var prop in propertyPath)
            {
                if (!current.TryGetProperty(prop, out var next))
                    return null;
                current = next;
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }

        private DateTime SafeGetDateTime(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(prop.GetString(), out var dt))
                    return dt;
            }
            return DateTime.MinValue;
        }

        private bool SafeGetBoolean(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True)
                return true;
            return false;
        }

        private List<string> SafeGetStringArray(JsonElement element, string propertyName)
        {
            var list = new List<string>();
            if (element.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arrayElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is string str)
                        list.Add(str);
                }
            }
            return list;
        }

        public async Task<CurrentTrackDto?> GetCurrentTrackForChannelAsync(string channelSlug)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{DR_API_BASE}/schedules/all/now");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var episodes = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();

                // Find the first episode for this channel with audio assets
                var episode = episodes.FirstOrDefault(e =>
                    SafeGetString(e, "channel", "slug") == channelSlug && 
                    e.TryGetProperty("audioAssets", out var assets));

                if (episode.ValueKind == JsonValueKind.Undefined)
                    return null;

                var channelTitle = SafeGetString(episode, "channel", "title");
                var programTitle = SafeGetString(episode, "title");

                if (!episode.TryGetProperty("audioAssets", out var audioAssets))
                    return null;

                // Find ICY format stream URL
                string? icyStreamUrl = null;
                foreach (var asset in audioAssets.EnumerateArray())
                {
                    var format = SafeGetString(asset, "format");
                    var target = SafeGetString(asset, "target");
                    var url = SafeGetString(asset, "url");
                    
                    if (format == "ICY" && target == "Stream" && !string.IsNullOrEmpty(url))
                    {
                        icyStreamUrl = url;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(icyStreamUrl))
                    return null;

                var icyResult = await GetCurrentTrackFromICYAsync(icyStreamUrl);
                return new CurrentTrackDto
                {
                    ChannelSlug = channelSlug,
                    ChannelTitle = channelTitle,
                    ProgramTitle = programTitle,
                    IcyStreamUrl = icyStreamUrl,
                    CurrentTrack = icyResult?.CurrentTrack,
                    MatchedMetadata = icyResult?.MatchedMetadata,
                    RawMetadataSamples = icyResult?.RawMetadataSamples ?? new List<string>()
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<IcyMetadataResult?> GetCurrentTrackFromICYAsync(string icyStreamUrl)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) })
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, icyStreamUrl);
                    request.Headers.Add("Icy-MetaData", "1");

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!response.IsSuccessStatusCode)
                            return null;

                        if (!response.Headers.TryGetValues("icy-metaint", out var metaintValues))
                            return null;

                        string? metaintStr = metaintValues.FirstOrDefault();
                        if (string.IsNullOrEmpty(metaintStr) || !int.TryParse(metaintStr, out var metaint) || metaint <= 0)
                            return null;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            var rawMetadataSamples = new List<string>();

                            for (var sampleIndex = 0; sampleIndex < 3; sampleIndex++)
                            {
                                if (!await SkipExactlyAsync(stream, metaint))
                                    break;

                                byte[] lengthByte = new byte[1];
                                if (!await ReadExactlyAsync(stream, lengthByte, 1))
                                    break;

                                int metadataLength = lengthByte[0] * 16;
                                if (metadataLength == 0)
                                    continue;

                                byte[] metadataBuffer = new byte[metadataLength];
                                if (!await ReadExactlyAsync(stream, metadataBuffer, metadataLength))
                                    continue;

                                string metadata = System.Text.Encoding.UTF8.GetString(metadataBuffer).TrimEnd('\0');
                                rawMetadataSamples.Add(metadata);
                                var match = Regex.Match(metadata, @"StreamTitle='([^']*)'");
                                if (!match.Success)
                                    continue;

                                var candidate = match.Groups[1].Value.Trim();
                                if (string.IsNullOrWhiteSpace(candidate) || candidate == "StreamTitle")
                                    continue;

                                if (!LooksLikeMusicTitle(candidate))
                                    continue;

                                return new IcyMetadataResult
                                {
                                    CurrentTrack = candidate,
                                    MatchedMetadata = metadata,
                                    RawMetadataSamples = rawMetadataSamples
                                };
                            }

                            return new IcyMetadataResult
                            {
                                CurrentTrack = null,
                                MatchedMetadata = rawMetadataSamples.LastOrDefault(),
                                RawMetadataSamples = rawMetadataSamples
                            };
                        }
                    }
                }
            }
            catch
            {
                return new IcyMetadataResult
                {
                    CurrentTrack = null,
                    MatchedMetadata = null,
                    RawMetadataSamples = new List<string>()
                };
            }
        }

        private static bool LooksLikeMusicTitle(string title)
        {
            var normalized = title.Trim();
            if (string.IsNullOrEmpty(normalized))
                return false;

            if (normalized.Contains("dr.dk", StringComparison.OrdinalIgnoreCase))
                return false;

            if (normalized.StartsWith("DR ", StringComparison.OrdinalIgnoreCase))
                return false;

            if (normalized.Contains("/") && !normalized.Contains(" - "))
                return false;

            var parts = normalized.Split(" - ", 2, StringSplitOptions.None);
            if (parts.Length != 2)
                return false;

            if (parts[0].Length > 50 || parts[1].Length > 60)
                return false;

            if (parts[1].Contains("dr.dk", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, int length)
        {
            var offset = 0;
            while (offset < length)
            {
                var bytesRead = await stream.ReadAsync(buffer, offset, length - offset);
                if (bytesRead == 0)
                    return false;

                offset += bytesRead;
            }

            return true;
        }

        private static async Task<bool> SkipExactlyAsync(Stream stream, int length)
        {
            var buffer = new byte[Math.Min(length, 4096)];
            var remaining = length;

            while (remaining > 0)
            {
                var toRead = Math.Min(buffer.Length, remaining);
                var bytesRead = await stream.ReadAsync(buffer, 0, toRead);
                if (bytesRead == 0)
                    return false;

                remaining -= bytesRead;
            }

            return true;
        }
    }

    public class ChannelNowPlayingDto
    {
        public string? ChannelSlug { get; set; }
        public string? ChannelTitle { get; set; }
        public NowPlayingDto NowPlaying { get; set; } = new();
    }

    public class NowPlayingDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> Categories { get; set; } = new();
        public string? SeriesTitle { get; set; }
        public string? PresentationUrl { get; set; }
        public bool IsAvailableOnDemand { get; set; }
        public string? CurrentTrack { get; set; }
    }

    public class CurrentTrackDto
    {
        public string? ChannelSlug { get; set; }
        public string? ChannelTitle { get; set; }
        public string? ProgramTitle { get; set; }
        public string? IcyStreamUrl { get; set; }
        public string? CurrentTrack { get; set; }
        public string? MatchedMetadata { get; set; }
        public List<string> RawMetadataSamples { get; set; } = new();
    }

    public class IcyMetadataResult
    {
        public string? CurrentTrack { get; set; }
        public string? MatchedMetadata { get; set; }
        public List<string> RawMetadataSamples { get; set; } = new();
    }
}
