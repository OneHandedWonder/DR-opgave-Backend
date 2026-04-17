using Microsoft.AspNetCore.Mvc;
using DR_Repo.Services;

namespace DR_Repo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RadioController : ControllerBase
    {
        private readonly DRRadioService _drRadioService;
        private readonly ILogger<RadioController> _logger;

        public RadioController(DRRadioService drRadioService, ILogger<RadioController> logger)
        {
            _drRadioService = drRadioService;
            _logger = logger;
        }

        [HttpGet("now-playing")]
        public async Task<ActionResult<List<ChannelNowPlayingDto>>> GetNowPlaying()
        {
            try
            {
                var nowPlaying = await _drRadioService.GetNowPlayingAllChannelsAsync();
                return Ok(nowPlaying);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching now playing data");
                return StatusCode(500, new { message = "Error fetching radio data", error = ex.Message });
            }
        }

        [HttpGet("now-playing/{channelSlug}/track")]
        public async Task<ActionResult<CurrentTrackDto>> GetCurrentTrack(string channelSlug)
        {
            try
            {
                var track = await _drRadioService.GetCurrentTrackForChannelAsync(channelSlug.ToLower());
                _logger.LogInformation($"Track for {channelSlug}: {track?.CurrentTrack ?? "null"}");
                return Ok(track);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current track");
                return StatusCode(500, new { message = "Error fetching track data", error = ex.Message });
            }
        }

        [HttpGet("now-playing/{channelSlug}/debug")]
        public async Task<ActionResult> DebugTrack(string channelSlug)
        {
            try
            {
                var allSchedules = await _drRadioService.GetNowPlayingAllChannelsAsync();
                var channelData = allSchedules.FirstOrDefault(c => c.ChannelSlug == channelSlug.ToLower());
                
                if (channelData == null)
                    return NotFound(new { message = $"Channel {channelSlug} not found" });

                return Ok(new 
                { 
                    channelSlug = channelSlug.ToLower(),
                    found = true,
                    channelTitle = channelData.ChannelTitle,
                    nowPlaying = channelData.NowPlaying
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug endpoint");
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }
    }
}
