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
            var states = _liteDb.GetCollection<TootState>();
            await states.DeleteAllAsync();
            _logger.LogDebug("Emptied Collection");
        }

        public async Task<TootState> GetLastToot()
        {
            var states = _liteDb.GetCollection<TootState>();
            if (await states.CountAsync() == 0) return null;
            var newest = await states.MaxAsync(q => q.Tooted);
            var lastToot = await states.FindOneAsync(q => q.Tooted == newest);
            return lastToot;
        }

        public async Task Add(TootState state)
        {
            var states = _liteDb.GetCollection<TootState>();
            await states.UpsertAsync(state);
            _logger.LogDebug("Added state");
        }

        public async Task RemoveOlderThan(DateTime maxAge)
        {
            var states = _liteDb.GetCollection<TootState>();
            await states.DeleteManyAsync(q => q.Tooted < maxAge);
            _logger.LogDebug("Removed old entries");
        }
    }
}