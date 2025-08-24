namespace YouToot
{
    public class TubeState
    {
        public int Id { get; set; }
        public required string MastodonId { get; set; }
        public required string YouTubeId { get; init; }
        public required string YouTubeChannel { get; init; }
        public DateTime Tooted { get; init; }
        public DateTime Published { get; set; }
    }
}