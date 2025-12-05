
namespace QuizBattle.Infrastructure.Features.Games.Redis
{

    internal static class LuaScripts
    {
        /// <summary>
        /// Creates a new game room atomically.
        /// KEYS[1] = room key (game:room:{id})
        /// KEYS[2] = active rooms set (game:active_rooms)
        /// ARGV[1] = room JSON
        /// ARGV[2] = room ID
        /// ARGV[3] = TTL in seconds
        /// Returns: "OK" or error
        /// </summary>
        public const string CreateRoom = """
        local roomKey = KEYS[1]
        local activeRoomsKey = KEYS[2]
        local roomJson = ARGV[1]
        local roomId = ARGV[2]
        local ttl = tonumber(ARGV[3])
        
        -- Check if room already exists
        if redis.call('EXISTS', roomKey) == 1 then
            return {err = 'ROOM_EXISTS'}
        end
        
        -- Create room
        redis.call('SET', roomKey, roomJson, 'EX', ttl)
        redis.call('SADD', activeRoomsKey, roomId)
        
        return 'OK'
        """;

        /// <summary>
        /// Joins a player to a room atomically.
        /// KEYS[1] = room key
        /// KEYS[2] = player-to-room mapping key (game:player:{userId})
        /// ARGV[1] = userId
        /// ARGV[2] = displayName
        /// ARGV[3] = photoUrl (can be empty)
        /// ARGV[4] = current timestamp ms
        /// ARGV[5] = TTL in seconds
        /// Returns: updated room JSON or error
        /// </summary>
        public const string JoinRoom = """
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
            return {err = 'ROOM_NOT_FOUND'}
        end
        
        local room = cjson.decode(roomJson)
        
        -- Check game status
        if room.status ~= 1 then  -- WaitingForPlayers = 1
            return {err = 'GAME_ALREADY_STARTED'}
        end
        
        -- Check max players
        if #room.players >= 5 then
            return {err = 'ROOM_FULL'}
        end
        
        -- Check if player already in room
        for _, player in ipairs(room.players) do
            if player.userId == userId then
                return {err = 'PLAYER_ALREADY_IN_ROOM'}
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
            photoUrl = photoUrl ~= '' and photoUrl or nil,
            colorHex = color.hex,
            colorName = color.name,
            totalScore = 0,
            currentRoundScore = 0,
            isReady = false,
            isConnected = true,
            currentAnswer = nil,
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
        
        return updatedJson
        """;

        /// <summary>
        /// Submits an answer atomically with race condition protection.
        /// KEYS[1] = room key
        /// ARGV[1] = userId
        /// ARGV[2] = answer
        /// ARGV[3] = current timestamp ms
        /// Returns: JSON with result info or error
        /// </summary>
        public const string SubmitAnswer = """
        local roomKey = KEYS[1]
        local userId = tonumber(ARGV[1])
        local answer = ARGV[2]
        local timestamp = tonumber(ARGV[3])
        
        -- Get current room state
        local roomJson = redis.call('GET', roomKey)
        if not roomJson then
            return {err = 'ROOM_NOT_FOUND'}
        end
        
        local room = cjson.decode(roomJson)
        
        -- Check game status
        if room.status ~= 3 then  -- RoundInProgress = 3
            return {err = 'ROUND_NOT_ACTIVE'}
        end
        
        -- Check if round expired
        if room.roundEndsAt and timestamp > room.roundEndsAt then
            return {err = 'ROUND_EXPIRED'}
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
            return {err = 'PLAYER_NOT_IN_ROOM'}
        end
        
        -- Check if already answered
        if player.currentAnswer then
            return {err = 'ALREADY_ANSWERED'}
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
                if p.currentAnswer then
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
        
        return cjson.encode(result)
        """;

        /// <summary>
        /// Ends the current round, calculates scores atomically.
        /// KEYS[1] = room key
        /// ARGV[1] = maxPoints
        /// ARGV[2] = pointDecrement
        /// Returns: round result JSON or error
        /// </summary>
        public const string EndRound = """
        local roomKey = KEYS[1]
        local maxPoints = tonumber(ARGV[1])
        local pointDecrement = tonumber(ARGV[2])
        
        -- Get current room state
        local roomJson = redis.call('GET', roomKey)
        if not roomJson then
            return {err = 'ROOM_NOT_FOUND'}
        end
        
        local room = cjson.decode(roomJson)
        
        -- Check game status
        if room.status ~= 3 then  -- RoundInProgress = 3
            return {err = 'ROUND_NOT_ACTIVE'}
        end
        
        -- Get current question
        local question = room.questions[room.currentRound]
        if not question then
            return {err = 'NO_QUESTION'}
        end
        
        -- Find correct answers and sort by response time
        local correctAnswers = {}
        local incorrectAnswers = {}
        
        for _, player in ipairs(room.players) do
            if player.currentAnswer and 
               string.upper(player.currentAnswer.answer) == string.upper(question.correctOption) then
                table.insert(correctAnswers, player)
            else
                table.insert(incorrectAnswers, player)
            end
        end
        
        -- Sort correct answers by response time
        table.sort(correctAnswers, function(a, b)
            return (a.currentAnswer.responseTimeMs or 999999) < (b.currentAnswer.responseTimeMs or 999999)
        end)
        
        -- Award points
        local playerResults = {}
        local points = maxPoints
        
        for _, player in ipairs(correctAnswers) do
            -- Find and update player in room
            for i, p in ipairs(room.players) do
                if p.userId == player.userId then
                    room.players[i].currentRoundScore = points
                    room.players[i].totalScore = room.players[i].totalScore + points
                    break
                end
            end
            
            table.insert(playerResults, {
                userId = player.userId,
                displayName = player.displayName,
                answerGiven = player.currentAnswer.answer,
                responseTimeMs = player.currentAnswer.responseTimeMs,
                pointsAwarded = points,
                isCorrect = true
            })
            
            points = math.max(100, points - pointDecrement)
        end
        
        -- Add incorrect/no answer players
        for _, player in ipairs(incorrectAnswers) do
            for i, p in ipairs(room.players) do
                if p.userId == player.userId then
                    room.players[i].currentRoundScore = 0
                    break
                end
            end
            
            table.insert(playerResults, {
                userId = player.userId,
                displayName = player.displayName,
                answerGiven = player.currentAnswer and player.currentAnswer.answer or nil,
                responseTimeMs = player.currentAnswer and player.currentAnswer.responseTimeMs or nil,
                pointsAwarded = 0,
                isCorrect = false
            })
        end
        
        -- Update room status
        room.status = 4  -- RoundEnded
        
        -- Build current standings
        local standings = {}
        for _, player in ipairs(room.players) do
            table.insert(standings, {
                userId = player.userId,
                displayName = player.displayName,
                totalScore = player.totalScore
            })
        end
        table.sort(standings, function(a, b) return a.totalScore > b.totalScore end)
        
        -- Save updated room
        local updatedJson = cjson.encode(room)
        redis.call('SET', roomKey, updatedJson, 'KEEPTTL')
        
        -- Build result
        local result = {
            roundNumber = room.currentRound,
            questionId = question.questionId,
            correctOption = question.correctOption,
            correctAnswerText = question['option' .. question.correctOption],
            playerResults = playerResults,
            currentStandings = standings
        }
        
        return cjson.encode(result)
        """;

        /// <summary>
        /// Matchmaking - adds player to queue and checks for match.
        /// KEYS[1] = queue key (matchmaking:{gameType}:{language})
        /// KEYS[2] = player info hash key
        /// ARGV[1] = userId
        /// ARGV[2] = displayName
        /// ARGV[3] = photoUrl
        /// ARGV[4] = required players count
        /// ARGV[5] = timestamp
        /// ARGV[6] = TTL
        /// Returns: matched players JSON or "WAITING"
        /// </summary>
        public const string JoinMatchmaking = """
        local queueKey = KEYS[1]
        local playerInfoKey = KEYS[2]
        local userId = ARGV[1]
        local displayName = ARGV[2]
        local photoUrl = ARGV[3]
        local requiredPlayers = tonumber(ARGV[4])
        local timestamp = tonumber(ARGV[5])
        local ttl = tonumber(ARGV[6])
        
        -- Store player info
        redis.call('HSET', playerInfoKey, 
            'displayName', displayName,
            'photoUrl', photoUrl or '',
            'joinedAt', timestamp)
        redis.call('EXPIRE', playerInfoKey, ttl)
        
        -- Add to queue with timestamp as score
        redis.call('ZADD', queueKey, timestamp, userId)
        redis.call('EXPIRE', queueKey, ttl)
        
        -- Check queue size
        local queueSize = redis.call('ZCARD', queueKey)
        
        if queueSize >= requiredPlayers then
            -- Get oldest players
            local matchedUserIds = redis.call('ZRANGE', queueKey, 0, requiredPlayers - 1)
            
            -- Remove from queue
            redis.call('ZREM', queueKey, unpack(matchedUserIds))
            
            -- Build matched players list
            local players = {}
            for _, odUserId in ipairs(matchedUserIds) do
                local infoKey = 'matchmaking:player:' .. odUserId
                local info = redis.call('HGETALL', infoKey)
                local playerInfo = {userId = tonumber(odUserId)}
                
                for i = 1, #info, 2 do
                    playerInfo[info[i]] = info[i + 1]
                end
                
                table.insert(players, playerInfo)
                
                -- Cleanup player info
                redis.call('DEL', infoKey)
            end
            
            return cjson.encode({matched = true, players = players})
        end
        
        return cjson.encode({matched = false, queuePosition = queueSize})
        """;

        /// <summary>
        /// Sets player ready status atomically.
        /// </summary>
        public const string SetPlayerReady = """
        local roomKey = KEYS[1]
        local userId = tonumber(ARGV[1])
        local isReady = ARGV[2] == 'true'
        
        local roomJson = redis.call('GET', roomKey)
        if not roomJson then
            return {err = 'ROOM_NOT_FOUND'}
        end
        
        local room = cjson.decode(roomJson)
        
        local found = false
        for i, player in ipairs(room.players) do
            if player.userId == userId then
                room.players[i].isReady = isReady
                found = true
                break
            end
        end
        
        if not found then
            return {err = 'PLAYER_NOT_IN_ROOM'}
        end
        
        local updatedJson = cjson.encode(room)
        redis.call('SET', roomKey, updatedJson, 'KEEPTTL')
        
        -- Check if all ready
        local allReady = #room.players >= 2
        for _, player in ipairs(room.players) do
            if not player.isReady then
                allReady = false
                break
            end
        end
        
        return cjson.encode({success = true, allPlayersReady = allReady})
        """;

        /// <summary>
        /// Starts the next round atomically.
        /// </summary>
        public const string StartNextRound = """
        local roomKey = KEYS[1]
        local timestamp = tonumber(ARGV[1])
        local roundDurationMs = tonumber(ARGV[2])
        
        local roomJson = redis.call('GET', roomKey)
        if not roomJson then
            return {err = 'ROOM_NOT_FOUND'}
        end
        
        local room = cjson.decode(roomJson)
        
        -- Check valid status
        if room.status ~= 2 and room.status ~= 4 then  -- Starting or RoundEnded
            return {err = 'INVALID_STATE'}
        end
        
        -- Check if more rounds available
        if room.currentRound >= room.totalRounds then
            return {err = 'NO_MORE_ROUNDS'}
        end
        
        -- Increment round
        room.currentRound = room.currentRound + 1
        room.status = 3  -- RoundInProgress
        room.roundStartedAt = timestamp
        room.roundEndsAt = timestamp + roundDurationMs
        
        -- Clear player answers
        for i, player in ipairs(room.players) do
            room.players[i].currentAnswer = nil
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
            question = clientQuestion,
            roundEndsAt = room.roundEndsAt,
            currentRound = room.currentRound,
            totalRounds = room.totalRounds
        })
        """;
    }
}
