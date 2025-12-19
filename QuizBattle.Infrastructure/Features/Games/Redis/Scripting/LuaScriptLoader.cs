using StackExchange.Redis;
using System.Collections.Concurrent;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting
{
    internal sealed class LuaScriptLoader
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly ConcurrentDictionary<string, LoadedScript> _scripts = new();
        private readonly string _scriptsPath;

        public LuaScriptLoader(IConnectionMultiplexer mux, string scriptsPath)
        {
            _mux = mux;
            _scriptsPath = Path.Combine(AppContext.BaseDirectory, scriptsPath);
        }

        public async Task LoadAllAsync(CancellationToken ct = default)
        {
            var dir = new DirectoryInfo(_scriptsPath);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Lua scripts directory not found: {_scriptsPath}");
            }

            var server = _mux.GetServer(_mux.GetEndPoints().First());

            foreach (var file in dir.GetFiles("*.lua"))
            {
                var text = await File.ReadAllTextAsync(file.FullName, ct);

                var loadedBytes = await server.ScriptLoadAsync(text);

                var sha = Convert.ToHexStringLower(loadedBytes);

                var scriptName = Path.GetFileNameWithoutExtension(file.Name);
                _scripts[scriptName.ToLowerInvariant()] =
                    new LoadedScript(scriptName, text, sha);
            }

            if (_scripts.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No Lua scripts were loaded from {_scriptsPath}");
            }

        }


        public bool TryGetScript(string scriptName, out LoadedScript? script)
        {
            return _scripts.TryGetValue(scriptName.ToLowerInvariant(), out script);
        }

        internal record LoadedScript(string Name, string Text, string Sha);
    }
}