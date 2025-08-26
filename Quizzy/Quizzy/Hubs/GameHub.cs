using Microsoft.AspNetCore.SignalR;
using Quizzy.Web.Models;
using Quizzy.Web.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Quizzy.Web.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService _games;
        public GameHub(GameService games) => _games = games;

        // ----- UTIL -----
        private async Task BroadcastRoomState(Room room)
        {
            // Transition: if a scheduled Q is due, start it server-side
            room.StartScheduledQuestionIfDue();

            RoomStateDto state = new RoomStateDto
            {
                RoomId = room.RoomId,
                Host = room.HostConnectionId,
                Players = room.Players.Values.Select(p => new { p.Name, p.Score, p.HasAnswered }).Cast<object>().ToArray(),
                Question = room.CurrentQuestion == null || room.QuestionStartTime == null ? null :
                    new RoomStateDto.QuestionBlock(
                        room.CurrentQuestion.Text,
                        room.CurrentQuestion.Options,
                        room.CurrentQuestion.DurationSeconds,
                        room.QuestionStartTime.Value
                    ),
                Upcoming = room.UpcomingQuestion == null || room.NextQuestionStartTime == null ? null :
                    new RoomStateDto.UpcomingBlock(
                        room.UpcomingQuestion.Text,
                        room.UpcomingQuestion.Options,
                        room.NextQuestionStartTime.Value
                    )
            };

            await Clients.Group(room.RoomId).SendAsync("RoomStateUpdated", state);
        }

        // ----- PLAYER -----
        public async Task JoinAsPlayer(string roomId, string playerName)
        {
            var room = _games.GetOrCreateRoom(roomId);
            room.Players[Context.ConnectionId] = new Player(Context.ConnectionId, playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
            await BroadcastRoomState(room);
        }

        public async Task SubmitAnswer(string roomId, int optionIndex)
        {
            if (!_games.TryGetRoom(roomId, out var room) || !room.QuestionActive || room.CurrentQuestion == null) return;
            if (!room.Players.TryGetValue(Context.ConnectionId, out var player) || player.HasAnswered) return;

            var elapsed = (int)(DateTimeOffset.UtcNow - room.QuestionStartTime!.Value).TotalMilliseconds;
            player.HasAnswered = true;
            player.LastAnswer = optionIndex;
            player.AnswerTimeMs = elapsed;

            var correct = optionIndex == room.CurrentQuestion.CorrectIndex;
            if (correct)
            {
                var remaining = Math.Max(0, room.CurrentQuestion.DurationSeconds * 1000 - elapsed);
                var points = 500 + remaining / 10;
                player.Score += points;
            }

            await BroadcastRoomState(room);
        }

        // ----- HOST -----
        public async Task ClaimHost(string roomId)
        {
            var room = _games.GetOrCreateRoom(roomId);
            room.HostConnectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
            await BroadcastRoomState(room);
        }

        public async Task StartQuestionNow(string roomId, string text, string[] options, int correctIndex, int durationSeconds)
        {
            if (!_games.TryGetRoom(roomId, out var room)) return;
            if (room.HostConnectionId != Context.ConnectionId) return;
            var q = new Question(text, options, correctIndex, durationSeconds);
            room.StartQuestion(q);
            await BroadcastRoomState(room);
        }

        public async Task ScheduleNextQuestion(string roomId, string text, string[] options, int correctIndex, int inSeconds)
        {
            if (!_games.TryGetRoom(roomId, out var room)) return;
            if (room.HostConnectionId != Context.ConnectionId) return;
            var q = new Question(text, options, correctIndex, DurationSeconds: 20);
            var startsAt = DateTimeOffset.UtcNow.AddSeconds(inSeconds);
            room.ScheduleNextQuestion(q, startsAt);
            await BroadcastRoomState(room);
        }

        public async Task EndQuestion(string roomId)
        {
            if (!_games.TryGetRoom(roomId, out var room) || room.CurrentQuestion == null) return;
            if (room.HostConnectionId != Context.ConnectionId) return;

            var q = room.CurrentQuestion;
            var optionCounts = new int[q.Options.Length];
            foreach (var p in room.Players.Values)
            {
                if (p.LastAnswer is int idx && idx >= 0 && idx < optionCounts.Length)
                    optionCounts[idx]++;
            }

            var leaderboard = room.Players.Values
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name)
                .Select(p => (p.Name, p.Score))
                .ToArray();

            room.EndQuestion();

            await Clients.Group(room.RoomId).SendAsync("QuestionEnded",
                new QuestionSummaryDto(q.CorrectIndex, optionCounts, leaderboard));

            await BroadcastRoomState(room);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _games.RemoveConnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}