namespace YouToot
{
    public class TubeState
    {
        public int Id { get; set; }
        public string MastodonId { get; set; }
        public string YouTubeId { get; set; }
        public DateTime Tooted { get; set; }
        public DateTime Published { get; set; }
    }
}