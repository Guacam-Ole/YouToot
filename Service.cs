using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using YoutubeExplode.Videos;

namespace YouToot
{
    public class Service
    {
        private const int MaxContentLength = 480;
        private const int MaxNumberOfVideosOnSearch = 10;
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
            _config = JsonConvert.DeserializeObject<Config>(config) ??
                      throw new FileNotFoundException("cannot read config");
        }

        public async Task TootNewVideos()
        {
            while (true)
            {
                foreach (var channel in _config.Channels.OrderByDescending(q => q.MaxAgeMonths))
                {
                    var sentToots = (await _database.GetSentTootsForChannel(channel.Url))?.ToList();
                    if (sentToots == null || !sentToots.Any())
                    {
                        _logger.LogWarning("Found no configs for '{url}'. Sending last one", channel.Url);
                        await TootLastVideos(channel, 1); // First run. Toot the latest video
                    }
                    else
                    {
                        await TootVideoSinceId(channel, sentToots.Select(q => q.YouTubeId).ToList());
                    }

                    await _database.RemoveOlderThan(channel.Url, DateTime.Now.AddMonths(-channel.MaxAgeMonths));
                }
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }

        private async Task TootLastVideos(Config.Channel channel, int numberOfVideos)
        {
            var videos = await _tube.GetVideos(channel, null, numberOfVideos);
            await TootVideos(channel, videos);
        }

        private async Task TootVideoSinceId(Config.Channel channel, List<string> videoIds)
        {
            var videos = await _tube.GetVideos(channel, videoIds, MaxNumberOfVideosOnSearch);
            await TootVideos(channel, videos);
        }

        private async Task TootVideos(Config.Channel channel, List<Video> videos)
        {
            if (videos.Count ==0) return;
            _logger.LogDebug("tooting {count} videos", videos.Count);
            foreach (var video in videos.OrderBy(q => q.UploadDate))
            {
                try
                {
                    var content =
                        $"{video.Author}{channel.Prefix}{video.Title}\n\n{video.Description}\n{video.Url}\n\n{GetHashTags(video.Keywords)}";
                    if (content.Length > MaxContentLength)
                    {
                        // Too long. Start by removing description
                        content =
                            $"{video.Author}{channel.Prefix}{video.Title}\n{video.Url}\n\n{GetHashTags(video.Keywords)}";
                    }

                    if (content.Length > MaxContentLength)
                    {
                        // still too long; remove tags + cut the end
                        content = $"{video.Author}{channel.Prefix}{video.Title}\n\n{video.Url}";
                    }

                    if (content.Length > MaxContentLength)
                    {
                        content = content[..MaxContentLength];
                    }

                    var status = await _toot.SendToot(_config.Instance, _config.AccessToken, content);
                    _logger.LogDebug("tooted toot '{title}' with {chars} chars. Id:{id}", video.Title, content?.Length,
                        status?.Id);
                    if (status != null)
                    {
                        await _database.Add(new TubeState
                        {
                            MastodonId = status.Id,
                            Published = video.UploadDate.UtcDateTime,
                            Tooted = DateTime.UtcNow,
                            YouTubeId = video.Id,
                            YouTubeChannel = channel.Url
                        });
                    }
                    else
                    {
                        _logger.LogWarning("unable to toot video '{title}' ('{id}')", video.Title, video.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send toot for video '{id}'|'{title}'. Ignoring", video.Id,
                        video.Title);
                }
            }
        }

        private static string GetHashTags(IReadOnlyList<string> tagList)
        {
            return string.Join(" ", tagList.Distinct().Select(q => "#" + Regex.Replace(q, "[^A-Za-z0-9äöüÄÖßÜ_]", "")));
        }
    }
}