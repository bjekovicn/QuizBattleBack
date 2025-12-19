-- ============================================================================
-- FILE: join_matchmaking.lua (Ultimate Optimized Version)
-- ============================================================================

local queueKey = KEYS[1]
local playerInfoKey = KEYS[2]

local userId = tonumber(ARGV[1])
local displayName = ARGV[2]
local photoUrl = ARGV[3] or ""
local requiredPlayers = tonumber(ARGV[4])
local timestamp = tonumber(ARGV[5])
local ttl = tonumber(ARGV[6])

-- Store player info 
redis.call("HSET", playerInfoKey,
    "displayName", displayName,
    "photoUrl", photoUrl,
    "joinedAt", timestamp      
)
redis.call("EXPIRE", playerInfoKey, ttl)

-- Add user to queue
redis.call("ZADD", queueKey, timestamp, userId)
redis.call("EXPIRE", queueKey, ttl)

local queueSize = redis.call("ZCARD", queueKey)

-- Not enough players -> simple response
if queueSize < requiredPlayers then
    return cjson.encode({
        success = true,
        matched = false,
        queuePosition = queueSize
    })
end

-- Grab oldest N players
local matchedIds = redis.call("ZRANGE", queueKey, 0, requiredPlayers - 1)
redis.call("ZREM", queueKey, unpack(matchedIds))

-- Build players array for JSON
local players = {}
setmetatable(players, cjson.empty_array_mt)

for _, idStr in ipairs(matchedIds) do
    local id = tonumber(idStr)
    local infoKey = "matchmaking:player:" .. idStr
    local raw = redis.call("HGETALL", infoKey)

    local player = {
        userId = id,
        displayName = "",
        photoUrl = "",
        joinedAt = 0
    }

    -- Parse Redis hash
    for i = 1, #raw, 2 do
        local k = raw[i]
        local v = raw[i + 1]

        if k == "joinedAt" then
            player.joinedAt = tonumber(v)
        else
            player[k] = v
        end
    end

    table.insert(players, player)
    redis.call("DEL", infoKey)
end

return cjson.encode({
    success = true,
    matched = true,
    players = players
})
