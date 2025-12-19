using StackExchange.Redis;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting
{

    internal sealed class LuaScriptExecutor
    {
        private readonly IDatabase _db;
        private readonly LuaScriptLoader _loader;

        public LuaScriptExecutor(IConnectionMultiplexer mux, LuaScriptLoader loader)
        {
            _db = mux.GetDatabase();
            _loader = loader;
        }

        public async Task<RedisResult> EvalAsync(
            string scriptName,
            RedisKey[] keys,
            RedisValue[] args)
        {
            if (!_loader.TryGetScript(scriptName, out var script) || script == null)
            {
                throw new InvalidOperationException($"Lua script not loaded: {scriptName}");
            }

            try
            {
                return await _db.ScriptEvaluateAsync(script.Sha, keys, args);
            }
            catch (RedisServerException ex) when (ex.Message?.Contains("NOSCRIPT") == true)
            {
                return await _db.ScriptEvaluateAsync(script.Text, keys, args);
            }
        }
    }
}
