-- ============================================================================
-- FILE: join_room.lua
-- ============================================================================
-- Joins a player to a room atomically.
-- KEYS[1] = room key
-- KEYS[2] = player-to-room mapping key (game:player:{userId})
-- ARGV[1] = userId
-- ARGV[2] = displayName
-- ARGV[3] = photoUrl (can be empty)
-- ARGV[4] = current timestamp ms
-- ARGV[5] = TTL in seconds
-- Returns: JSON {success: true, player: {...}, room: {...}} or {success: false, error: "..."}

local roomKey = KEYS[1]
local playerKey = KEYS[2]
local userId = tonumber(ARGV[1])
local displayName = ARGV[2]
local photoUrl = ARGV[3]
local timestamp = tonumber(ARGV[4])
local ttl = tonumber(ARGV[5])

-- Get current room state
local roomJson = redis.call('GET', roomKey)
if not roomJson then
    return cjson.encode({success = false, error = 'ROOM_NOT_FOUND'})
end

local room = cjson.decode(roomJson)

-- Check game status
if room.status ~= 1 then  -- WaitingForPlayers = 1
    return cjson.encode({success = false, error = 'GAME_ALREADY_STARTED'})
end

-- Check max players
if #room.players >= 5 then
    return cjson.encode({success = false, error = 'ROOM_FULL'})
end

-- Check if player already in room
for _, player in ipairs(room.players) do
    if player.userId == userId then
        return cjson.encode({success = false, error = 'PLAYER_ALREADY_IN_ROOM'})
    end
end

-- Assign color based on player count
local colors = {
    {hex = '#FF4038', name = 'Red'},
    {hex = '#2279EE', name = 'Blue'},
    {hex = '#FFE716', name = 'Yellow'},
    {hex = '#2DF726', name = 'Green'},
    {hex = '#9B59B6', name = 'Purple'}
}
local color = colors[#room.players + 1]

-- Create new player
local newPlayer = {
    userId = userId,
    displayName = displayName,
    photoUrl = photoUrl ~= '' and photoUrl or cjson.null,
    colorHex = color.hex,
    colorName = color.name,
    totalScore = 0,
    currentRoundScore = 0,
    isReady = false,
    isConnected = true,
    currentAnswer = cjson.null,
    joinedAt = timestamp
}

-- Add player to room
table.insert(room.players, newPlayer)

-- Set host if first player
if #room.players == 1 then
    room.hostPlayerId = userId
end

-- Save updated room
local updatedJson = cjson.encode(room)
redis.call('SET', roomKey, updatedJson, 'EX', ttl)

-- Map player to room
redis.call('SET', playerKey, room.id, 'EX', ttl)

return cjson.encode({success = true, player = newPlayer, room = room})

