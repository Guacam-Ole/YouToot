using Microsoft.Extensions.Logging;

using YoutubeExplode;
using YoutubeExplode.Playlists;

namespace YouToot
{
    public class Tube
    {
        private readonly ILogger<Tube> _logger;
        private readonly YoutubeClient _youtubeClient;

        public Tube(ILogger<Tube> logger)
        {
            _youtubeClient = new YoutubeClient();
            _logger = logger;
        }

        private async Task<List<PlaylistVideo>> GetVideosFromChannel(string channelUrl, List<string>? sinceIds, int? maxNumberOfVideos)
        {
            var itemCount = 0;
            var ytChannel = await _youtubeClient.Channels.GetByHandleAsync(channelUrl);
            var videos = new List<PlaylistVideo>();

            await foreach (var upload in _youtubeClient.Channels.GetUploadsAsync(ytChannel.Id))
            {
                if (sinceIds != null && sinceIds.Contains(upload.Id)) return videos;
                videos.Add(upload);
                _logger.LogDebug("Added '{title}' with id {id} to list of Videos [{duration}]", upload.Title, upload.Id, upload.Duration);
                itemCount++;
                if (maxNumberOfVideos != null && itemCount >= maxNumberOfVideos) return videos;
            }

            return sinceIds != null ? throw new ArgumentException("No video with that Id exists. To prevent accidental spamming no videos will be tooted") : videos;
        }

        public async Task<List<YoutubeExplode.Videos.Video>> GetVideos(Config.Channel channel, List<string>? sinceId, int? maxNumberOfVideos)
        {
            var retryCount = 5;

            while (retryCount > 0)
            {
                try
                {
                    retryCount--;
                    var videos = new List<YoutubeExplode.Videos.Video>();
                    var playlistVideos = await GetVideosFromChannel(channel.Url, sinceId, maxNumberOfVideos);
                    _logger.LogDebug("retrieved {Count} videos from channel '{Url}' since '{Since}'",
                        playlistVideos.Count, channel.Url, sinceId);
                    foreach (var video in playlistVideos)
                    {
                        videos.Add(await _youtubeClient.Videos.GetAsync(video.Id));
                    }

                    return videos;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed retrieving Videos from Channel '{Channel}' since '{Since}'. Retries left: {retryCount}", channel.Url, sinceId, retryCount);
                    Thread.Sleep(1000 * 30); // wait a few seconds
                }
            }
            return [];
        }
    }
}
