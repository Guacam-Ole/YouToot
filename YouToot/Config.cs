// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace YouToot
{
    public class Config
    {
        public string? Instance { get; set; }
        public string? AccessToken { get; set; }
        public List<Channel> Channels { get; set; } = []; 

        
        public class Channel
        {
            public int MaxAgeMonths { get; set; }
            public required string Url { get; set; }
            public required string Prefix { get; set; }
            public string[]? RemoveHashtags { get; set; }
        }
    }
}