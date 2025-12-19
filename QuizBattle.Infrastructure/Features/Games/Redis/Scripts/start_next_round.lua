
-- ============================================================================
-- FILE: start_next_round.lua
-- ============================================================================
-- Starts the next round atomically.
-- KEYS[1] = room key
-- ARGV[1] = timestamp
-- ARGV[2] = roundDurationMs
-- Returns: JSON {success: true, question: {...}, roundEndsAt: ..., currentRound: ..., totalRounds: ...} or {success: false, error: "..."}

local roomKey = KEYS[1]
local timestamp = tonumber(ARGV[1])
local roundDurationMs = tonumber(ARGV[2])

local roomJson = redis.call('GET', roomKey)
if not roomJson then
    return cjson.encode({success = false, error = 'ROOM_NOT_FOUND'})
end

local room = cjson.decode(roomJson)

-- Check valid status
if room.status ~= 2 and room.status ~= 4 then  -- Starting or RoundEnded
    return cjson.encode({success = false, error = 'INVALID_STATE'})
end

-- Check if more rounds available
if room.currentRound >= room.totalRounds then
    return cjson.encode({success = false, error = 'NO_MORE_ROUNDS'})
end

if not room.questions or #room.questions == 0 then
    return cjson.encode({success = false, error = 'NO_QUESTIONS_LOADED'})
end

-- Increment round
room.currentRound = room.currentRound + 1
room.status = 3  -- RoundInProgress
room.roundStartedAt = timestamp
room.roundEndsAt = timestamp + roundDurationMs

-- Clear player answers
for i, player in ipairs(room.players) do
    room.players[i].currentAnswer = cjson.null
    room.players[i].currentRoundScore = 0
end

local updatedJson = cjson.encode(room)
redis.call('SET', roomKey, updatedJson, 'KEEPTTL')

-- Return current question (without correct answer for client)
local question = room.questions[room.currentRound]
local clientQuestion = {
    questionId = question.questionId,
    roundNumber = question.roundNumber,
    text = question.text,
    optionA = question.optionA,
    optionB = question.optionB,
    optionC = question.optionC
    -- correctOption intentionally omitted
}

return cjson.encode({
    success = true,
    question = clientQuestion,
    roundEndsAt = room.roundEndsAt,
    currentRound = room.currentRound,
    totalRounds = room.totalRounds
})

