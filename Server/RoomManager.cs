using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace AOG.Server
{
    public sealed class RoomManager
    {
        private readonly ConcurrentDictionary<string, GameRoom> rooms = new();

        public async Task HandleMessageAsync(ClientSession session, NetMessage msg)
        {
            switch (msg.Type)
            {
                case NetMessageType.CreateRoom:
                    await HandleCreateRoomAsync(session);
                    break;

                case NetMessageType.JoinRoom:
                    await HandleJoinRoomAsync(session, msg.Payload);
                    break;

                case NetMessageType.Ready:
                    await HandleReadyAsync(session, msg.Payload);
                    break;

                case NetMessageType.PlayerInput:
                case NetMessageType.SyncHealth:
                case NetMessageType.GameOver:
                    // Chuyển tiếp ngay lập tức sang cho đối thủ trong phòng!
                    if (session.CurrentRoom != null)
                    {
                        await session.CurrentRoom.RelayToOpponentAsync(session, msg);
                    }
                    break;

                default:
                    Console.WriteLine($"[Lạ] Nhận gói tin chưa hỗ trợ: {msg.Type}");
                    break;
            }
        }

        private async Task HandleCreateRoomAsync(ClientSession session)
        {
            // Rời phòng cũ nếu đang ở phòng nào đó
            LeaveCurrentRoom(session);

            string roomId;
            do
            {
                roomId = Random.Shared.Next(1000, 9999).ToString();
            }
            while (rooms.ContainsKey(roomId));

            GameRoom room = new GameRoom(roomId, session);
            rooms[roomId] = room;

            Console.WriteLine($"[Tạo phòng] Player {session.SessionId} đã tạo phòng mã: {roomId}");
            await session.SendAsync(NetMessageType.RoomCreated, new CreateRoomResult
            {
                RoomId = roomId,
                PlayerIndex = 0
            });
        }

        private async Task HandleJoinRoomAsync(ClientSession session, string payload)
        {
            LeaveCurrentRoom(session);

            var req = JsonSerializer.Deserialize<JoinRoomRequest>(payload);
            string roomId = req?.RoomId?.Trim() ?? string.Empty;

            if (!rooms.TryGetValue(roomId, out GameRoom? room))
            {
                await session.SendAsync(NetMessageType.Error, "Phòng không tồn tại hoặc đã đóng!");
                return;
            }

            if (!room.TryJoin(session))
            {
                await session.SendAsync(NetMessageType.Error, "Phòng đã đủ 2 người chơi!");
                return;
            }

            Console.WriteLine($"[Vào phòng] Player {session.SessionId} đã vào phòng {roomId}");

            // Gửi xác nhận cho người vừa vào
            await session.SendAsync(NetMessageType.RoomJoined, new JoinRoomResult
            {
                RoomId = roomId,
                PlayerIndex = 1
            });

            // Báo cho chủ phòng biết đã có đối thủ vào
            if (room.Host != null)
            {
                await room.Host.SendAsync(NetMessageType.PlayerJoined, new { playerIndex = 1 });
            }
        }

        private async Task HandleReadyAsync(ClientSession session, string payload)
        {
            if (session.CurrentRoom == null) return;

            var readyData = JsonSerializer.Deserialize<PlayerReadyData>(payload);
            session.Loadout = readyData;

            Console.WriteLine($"[Sẵn sàng] Player {session.PlayerIndex} phòng {session.CurrentRoom.RoomId} đã sẵn sàng.");

            await session.CurrentRoom.CheckStartGameAsync();
        }

        public void OnClientDisconnected(ClientSession session)
        {
            LeaveCurrentRoom(session);
        }

        private void LeaveCurrentRoom(ClientSession session)
        {
            GameRoom? room = session.CurrentRoom;
            if (room != null)
            {
                room.RemovePlayer(session);
                Console.WriteLine($"[Rời phòng] Player {session.SessionId} rời phòng {room.RoomId}");

                ClientSession? opponent = room.Host ?? room.Guest;
                if (opponent != null)
                {
                    _ = opponent.SendAsync(NetMessageType.PlayerLeft, new { playerIndex = session.PlayerIndex });
                }

                if (room.IsEmpty)
                {
                    rooms.TryRemove(room.RoomId, out _);
                    Console.WriteLine($"[Hủy phòng] Phòng {room.RoomId} đã đóng vì không còn ai.");
                }
            }
        }
    }
}
