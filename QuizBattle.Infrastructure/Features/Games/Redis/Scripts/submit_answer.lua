-- ============================================================================
-- FILE: submit_answer.lua
-- ============================================================================
-- Submits an answer atomically with race condition protection.
-- KEYS[1] = room key
-- ARGV[1] = userId
-- ARGV[2] = answer
-- ARGV[3] = current timestamp ms
-- Returns: JSON {success: true, result: {...}} or {success: false, error: "..."}

local roomKey = KEYS[1]
local userId = tonumber(ARGV[1])
local answer = ARGV[2]
local timestamp = tonumber(ARGV[3])

-- Get current room state
local roomJson = redis.call('GET', roomKey)
if not roomJson then
    return cjson.encode({success = false, error = 'ROOM_NOT_FOUND'})
end

local room = cjson.decode(roomJson)

-- Check game status
if room.status ~= 3 then  -- RoundInProgress = 3
    return cjson.encode({success = false, error = 'ROUND_NOT_ACTIVE'})
end

-- Check if round expired
if room.roundEndsAt and timestamp > room.roundEndsAt then
    return cjson.encode({success = false, error = 'ROUND_EXPIRED'})
end

-- Find player
local playerIndex = nil
local player = nil
for i, p in ipairs(room.players) do
    if p.userId == userId then
        playerIndex = i
        player = p
        break
    end
end

if not player then
    return cjson.encode({success = false, error = 'PLAYER_NOT_IN_ROOM'})
end

-- Check if already answered
if player.currentAnswer ~= cjson.null and player.currentAnswer ~= nil then
    return cjson.encode({success = false, error = 'ALREADY_ANSWERED'})
end

-- Calculate response time
local responseTimeMs = timestamp - room.roundStartedAt

-- Set answer
room.players[playerIndex].currentAnswer = {
    answer = answer,
    responseTimeMs = responseTimeMs,
    answeredAt = timestamp
}

-- Count answered players
local answeredCount = 0
local connectedCount = 0
for _, p in ipairs(room.players) do
    if p.isConnected then
        connectedCount = connectedCount + 1
        if p.currentAnswer ~= cjson.null and p.currentAnswer ~= nil then
            answeredCount = answeredCount + 1
        end
    end
end

local allAnswered = answeredCount >= connectedCount

-- Save updated room
local updatedJson = cjson.encode(room)
redis.call('SET', roomKey, updatedJson, 'KEEPTTL')

-- Return result
local result = {
    accepted = true,
    allPlayersAnswered = allAnswered,
    playersAnsweredCount = answeredCount,
    totalPlayersCount = connectedCount
}

return cjson.encode({success = true, result = result})