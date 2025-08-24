using Mastonet;
using Mastonet.Entities;

using Microsoft.Extensions.Logging;

namespace YouToot
{
    public class Toot
    {
        private readonly ILogger<Toot> _logger;

        public Toot(ILogger<Toot> logger)
        {
            _logger = logger;
        }

        public async Task<Status?> SendToot(string instance, string secret, string content)
        {
            var client = new MastodonClient(instance, secret);
            if (client == null)
            {
                _logger.LogWarning("Bot not found or disabled");
                return null;
            }

            var status = await client.PublishStatus(content, Visibility.Public);
            _logger.LogDebug("Toot '{TootId}' sent with {chars} Chars", status.Id, content.Length);
            return status;
        }
    }
}