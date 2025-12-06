-- ============================================================================
-- FILE: end_round.lua
-- ============================================================================
-- Ends the current round atomically with race condition protection.
-- KEYS[1] = room key
-- ARGV[1] = current timestamp ms
-- Returns: JSON {success: true, result: {...}} or {success: false, error: "..."}

local roomKey = KEYS[1]
local timestamp = tonumber(ARGV[1])

-- Get current room state
local roomJson = redis.call('GET', roomKey)
if not roomJson then
    return cjson.encode({success = false, error = 'ROOM_NOT_FOUND'})
end

local room = cjson.decode(roomJson)

-- ✅ ATOMIC CHECK: Only proceed if status is RoundInProgress
if room.status ~= 3 then  -- RoundInProgress = 3
    return cjson.encode({success = false, error = 'ROUND_NOT_ACTIVE'})
end

-- Get current question
if room.currentRound <= 0 or room.currentRound > #room.questions then
    return cjson.encode({success = false, error = 'NO_QUESTION'})
end

local question = room.questions[room.currentRound]

-- Separate players into correct and incorrect answerers
local correctAnswers = {}
local incorrectAnswers = {}

for _, player in ipairs(room.players) do
    local hasAnswer = player.currentAnswer ~= cjson.null and player.currentAnswer ~= nil
    local isCorrect = hasAnswer and 
                      string.upper(player.currentAnswer.answer) == string.upper(question.correctOption)
    
    if isCorrect then
        table.insert(correctAnswers, player)
    else
        table.insert(incorrectAnswers, player)
    end
end

-- Sort correct answers by response time (ascending)
table.sort(correctAnswers, function(a, b)
    local aTime = a.currentAnswer and a.currentAnswer.responseTimeMs or 999999
    local bTime = b.currentAnswer and b.currentAnswer.responseTimeMs or 999999
    return aTime < bTime
end)

-- Award points - first correct gets 1000, each subsequent gets 150 less (min 100)
local playerResults = {}
setmetatable(playerResults, cjson.empty_array_mt)

local maxPoints = 1000
local pointDecrement = 150
local minPoints = 100
local currentPoints = maxPoints

-- Process correct answers
for _, player in ipairs(correctAnswers) do
    -- Award points
    player.currentRoundScore = currentPoints
    player.totalScore = player.totalScore + currentPoints
    
    -- Build result entry
    table.insert(playerResults, {
        userId = player.userId,
        displayName = player.displayName,
        answerGiven = player.currentAnswer.answer,
        responseTimeMs = player.currentAnswer.responseTimeMs,
        pointsAwarded = currentPoints,
        isCorrect = true
    })
    
    -- Decrease points for next player (minimum 100)
    currentPoints = math.max(minPoints, currentPoints - pointDecrement)
end

-- Process incorrect/no answers
for _, player in ipairs(incorrectAnswers) do
    player.currentRoundScore = 0
    
    local answerGiven = cjson.null
    local responseTimeMs = cjson.null
    
    if player.currentAnswer ~= cjson.null and player.currentAnswer ~= nil then
        answerGiven = player.currentAnswer.answer
        responseTimeMs = player.currentAnswer.responseTimeMs
    end
    
    table.insert(playerResults, {
        userId = player.userId,
        displayName = player.displayName,
        answerGiven = answerGiven,
        responseTimeMs = responseTimeMs,
        pointsAwarded = 0,
        isCorrect = false
    })
end

-- Build current standings (sorted by total score descending)
local standings = {}
setmetatable(standings, cjson.empty_array_mt)

-- Copy players array for sorting
local playersCopy = {}
for i, player in ipairs(room.players) do
    playersCopy[i] = player
end

-- Sort by total score descending
table.sort(playersCopy, function(a, b)
    return a.totalScore > b.totalScore
end)

for _, player in ipairs(playersCopy) do
    table.insert(standings, {
        userId = player.userId,
        displayName = player.displayName,
        totalScore = player.totalScore
    })
end

-- Get correct answer text
local correctAnswerText = question.optionA  -- Default
if question.correctOption == "B" then
    correctAnswerText = question.optionB
elseif question.correctOption == "C" then
    correctAnswerText = question.optionC
end

-- Build round result
local roundResult = {
    roundNumber = room.currentRound,
    questionId = question.questionId,
    correctOption = question.correctOption,
    correctAnswerText = correctAnswerText,
    playerResults = playerResults,
    currentStandings = standings
}

-- ✅ ATOMICALLY UPDATE STATUS
room.status = 4  -- RoundEnded

-- Save updated room
local updatedJson = cjson.encode(room)
redis.call('SET', roomKey, updatedJson, 'KEEPTTL')

return cjson.encode({success = true, result = roundResult})