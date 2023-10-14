using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Text.RegularExpressions;

using YoutubeExplode.Videos;

namespace YouToot
{
    public class Service
    {
        private const int _maxContentLength = 480;
        private readonly ILogger<Service> _logger;
        private readonly Tube _tube;
        private readonly Database _database;
        private readonly Toot _toot;
        private readonly Config _config;

        public Service(ILogger<Service> logger, Tube tube, Database database, Toot toot)
        {
            _logger = logger;
            _tube = tube;
            _database = database;
            _toot = toot;
            var config = File.ReadAllText("./config.json");
            _config = JsonConvert.DeserializeObject<Config>(config) ?? throw new FileNotFoundException("cannot read config");
        }

        public async Task TootNewVIdeos()
        {
            var lastToot = await _database.GetLastToot();
            if (lastToot == null)
            {
                await TootLastVideos(1);  // First run. Toot the latest video
            }
            else
            {
                await TootVideoSinceId(lastToot.YouTubeId);
            }
            await _database.RemoveOlderThan(DateTime.Now.AddMonths(-_config.MaxAgeMonths));
        }

        public async Task TootLastVideos(int numberOfVideos)
        {
            var videos = await _tube.GetVideos(_config.Url, null, numberOfVideos);
            await TootVideos(videos);
        }

        public async Task TootVideoSinceId(string videoId)
        {
            var videos = await _tube.GetVideos(_config.Url, videoId, null);
            await TootVideos(videos);
        }

        private async Task TootVideos(List<Video> videos)
        {
            _logger.LogDebug("tooting {count} videos", videos.Count);
            foreach (var video in videos.OrderBy(q => q.Id))
            {
                string content = $"{video.Author}{_config.Prefix}{video.Title}\n\n{video.Description}\n{video.Url}\n\n{GetHashTags(video.Keywords)}";
                if (content.Length > _maxContentLength)
                {
                    // Too long. Start by removing description
                    content = $"{video.Author}{_config.Prefix}{video.Title}\n{video.Url}\n\n{GetHashTags(video.Keywords)}";
                }
                if (content.Length > _maxContentLength)
                {
                    // still too long; remove tags + cut the end
                    content = $"{video.Author}{_config.Prefix}{video.Title}\n\n{video.Url}";
                }
                if (content.Length > _maxContentLength)
                {
                    content = content[.._maxContentLength];
                }
                var status = await _toot.SendToot(_config.Instance, _config.AccessToken, content);
                if (status != null)
                {
                    await _database.Add(new TootState
                    {
                        MastodonId = status.Id,
                        Published = video.UploadDate.UtcDateTime,
                        Tooted = DateTime.UtcNow,
                        YouTubeId = video.Id
                    });
                }
                else
                {
                    _logger.LogWarning("unable to toot video '{}title' ('{id}')", video.Title, video.Id);
                }
            }
        }

        private string GetHashTags(IReadOnlyList<string> tagList)
        {
            return string.Join(" ", tagList.Distinct().Select(q => "#" + Regex.Replace(q, "[^A-Za-z0-9äöüÄÖßÜ_]", "")));
        }
    }
}