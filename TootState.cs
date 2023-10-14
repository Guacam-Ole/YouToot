using System.Runtime.Remoting;
using System.Text.Json.Serialization;

namespace YouToot
{
    public class TootState
    {
        [JsonPropertyName("id")]
        public string MastodonId { get; set; }
        public string YouTubeId { get; set; }
        public DateTime Tooted { get; set; }
        public DateTime Published { get; set; }
    }
}