using System.Collections.Concurrent;
using Quizzy.Web.Models;

namespace Quizzy.Web.Services
{
    public class GameService
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();

        public Room GetOrCreateRoom(string roomId)
        {
            roomId = roomId.ToUpperInvariant();
            return _rooms.GetOrAdd(roomId, id => new Room { RoomId = id });
        }

        public bool TryGetRoom(string roomId, out Room room)
            => _rooms.TryGetValue(roomId.ToUpperInvariant(), out room);

        public void RemoveConnection(string connectionId)
        {
            foreach (var keyValue in _rooms)
            {
                var room = keyValue.Value;
                if (room.Players.Remove(connectionId)) { }
                if (room.HostConnectionId == connectionId) room.HostConnectionId = string.Empty;
            }
        }
    }
}