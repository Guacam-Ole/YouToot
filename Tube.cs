using Microsoft.Extensions.Logging;

using YoutubeExplode;
using YoutubeExplode.Playlists;

namespace YouToot
{
    public class Tube
    {
        private const string _ytUrl = "https://youtube.com/";
        private readonly ILogger<Tube> _logger;
        private YoutubeClient _youtubeClient;

        public Tube(ILogger<Tube> logger)
        {
            _youtubeClient = new YoutubeClient();
            _logger = logger;
        }

        private async Task<List<PlaylistVideo>> GetVideosFromChannel(string channelUrl, List<string> sinceIds, int? maxNumberOfVideos)
        {

            _logger.LogDebug("Getting Videos from channel '{url}'. Max number: '{max}', since id:'{id}'", channelUrl, maxNumberOfVideos, sinceIds);

            int itemCount = 0;
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

            if (sinceIds != null) throw new ArgumentException("No video with that Id exists. To prevent accidental spamming no videos will be tooted");
            return videos;
        }


        public async Task<List<YoutubeExplode.Videos.Video>> GetVideos(string channelUrl, List<string> sinceId, int? maxNumberOfVideos)
        {
            try
            {
                var videos = new List<YoutubeExplode.Videos.Video>();
                var playlistVideos = await GetVideosFromChannel(channelUrl, sinceId, maxNumberOfVideos);
                foreach (var video in playlistVideos)
                {
                    videos.Add(await _youtubeClient.Videos.GetAsync(video.Id));
                }
                return videos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving Videos");
                return new List<YoutubeExplode.Videos.Video>();
            }
        }
    }



    
}