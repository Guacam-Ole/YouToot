namespace YouToot
{
    public class Config
    {
        public string? Instance { get; set; }
        public string? AccessToken { get; set; }
        public List<Channel> Channels { get; set; } = new(); 

        public class Channel
        {
            public int MaxAgeMonths { get; set; }
            public string? Url { get; set; }
            public string? Prefix { get; set; }
        }
    }
}