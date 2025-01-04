using LiteDB.Async;
using Microsoft.Extensions.Logging;

namespace YouToot
{
    public class Database
    {
        private const string _datebaseFilename = "youtoot.db";

        public Database(ILogger<Database> logger)
        {
            _liteDb = new LiteDatabaseAsync(_datebaseFilename);
            _logger = logger;
        }

        private readonly ILogger<Database> _logger;
        private LiteDatabaseAsync _liteDb;

        public async Task Empty()
        {
            var states = _liteDb.GetCollection<TubeState>();
            await states.DeleteAllAsync();
            _logger.LogDebug("Emptied Collection");
        }

        public async Task<IEnumerable<TubeState>?> GetSentTootsForChannel(string channel)
        {
            var states = _liteDb.GetCollection<TubeState>();
            if (await states.CountAsync() == 0) return null;
            return await states.FindAsync(q => q.YouTubeChannel == channel);
        }

        public async Task Add(TubeState state)
        {
            var states = _liteDb.GetCollection<TubeState>();
            await states.UpsertAsync(state);
            _logger.LogDebug("Added state");
        }

        public async Task RemoveOlderThan(string channelUrl, DateTime maxAge)
        {
            var states = _liteDb.GetCollection<TubeState>();
            var count = await states.DeleteManyAsync(q => q.YouTubeChannel == channelUrl && q.Tooted < maxAge);
            _logger.LogDebug("Removed {count} old entries", count);
        }
    }
}