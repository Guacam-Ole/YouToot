using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
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
                    if (sentToots == null || sentToots.Count == 0)
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
                    var keywords = video.Keywords.ToList();
                    if (channel.RemoveHashtags != null)
                    {
                        keywords.RemoveAll(tag =>
                            channel.RemoveHashtags.Any(rem =>
                                tag.Contains(rem, StringComparison.CurrentCultureIgnoreCase)));
                    }
                    
                    var hashtags = GetHashTags(keywords);
              
                    var content =
                        $"{channel.Prefix}{video.Title}\n\n{video.Description}\n{video.Url}\n\n{hashtags}";
                    
                    
                    if (content.Length > MaxContentLength)
                    {
                        // Too long. Shorten the description:
                        var lengthWithOutDescription = content.Length - video.Description.Length;
                        if (lengthWithOutDescription > MaxContentLength)
                        {
                            content = $"{channel.Prefix}{video.Title}\n\n{video.Url}\n{hashtags}";     
                        }
                        else
                        {
                            var maxDescriptionLength = MaxContentLength - lengthWithOutDescription;
                            content=$"{channel.Prefix}{video.Title}\n\n{video.Description[..maxDescriptionLength]} \n\n {video.Url}\n{hashtags}";
                        }
                    }

                    // Remove HashTags until length ok
                    while (content.Length>MaxContentLength)
                    {
                        content = content[..content.LastIndexOf(' ')];
                    }
                    var status =await _toot.SendToot(_config.Instance!, _config.AccessToken!, content);
                   
                    if (status != null)
                    {
                        _logger.LogDebug("tooted toot '{Title}' with {CharCount} chars. Id:{Id}", video.Title, content.Length,
                            status.Id);
                        await _database.Add(new TubeState
                        {
                            MastodonId = status.Id,
                            Published = video.UploadDate.UtcDateTime,
                            Tooted = DateTime.UtcNow,
                            YouTubeId = video.Id,
                            YouTubeChannel = channel.Url!
                        });
                    }
                    else
                    {
                        _logger.LogWarning("unable to toot video '{Title}' ('{Id}')", video.Title, video.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send toot for video '{Id}'|'{Title}'. Ignoring", video.Id,
                        video.Title);
                }
            }
        }

        private static string GetHashTags(IReadOnlyList<string> tagList)
        {
             return string.Join(" ", tagList.Where(tag=>!tag.Contains("beans", StringComparison.CurrentCultureIgnoreCase) && !tag.Contains("rbtv", StringComparison.CurrentCultureIgnoreCase)).Select(q => "#" + Regex.Replace(q, "[^A-Za-z0-9äöüÄÖßÜ_]", "")));
        }
    }
}