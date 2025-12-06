using StackExchange.Redis;
using System.Collections.Concurrent;

namespace QuizBattle.Infrastructure.Features.Games.Redis;

internal sealed class LuaScriptLoader
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ConcurrentDictionary<string, LoadedScript> _scripts = new();
    private readonly string _scriptsPath;

    public LuaScriptLoader(IConnectionMultiplexer mux, string scriptsPath = "LuaScripts/room")
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
    }


    public bool TryGetScript(string scriptName, out LoadedScript? script)
    {
        return _scripts.TryGetValue(scriptName.ToLowerInvariant(), out script);
    }

    internal record LoadedScript(string Name, string Text, string Sha);
}

internal sealed class LuaScriptCaller
{
    private readonly IDatabase _db;
    private readonly LuaScriptLoader _loader;

    public LuaScriptCaller(IConnectionMultiplexer mux, LuaScriptLoader loader)
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