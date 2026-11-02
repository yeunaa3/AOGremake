using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AOG.Server
{
    public sealed class GameRoom
    {
        public string RoomId { get; }
        public ClientSession? Host { get; private set; }
        public ClientSession? Guest { get; private set; }

        public bool IsFull => Host != null && Guest != null;
        public bool IsEmpty => Host == null && Guest == null;

        public GameRoom(string roomId, ClientSession host)
        {
            RoomId = roomId;
            Host = host;
            Host.CurrentRoom = this;
            Host.PlayerIndex = 0;
        }

        public bool TryJoin(ClientSession guest)
        {
            if (IsFull) return false;

            Guest = guest;
            Guest.CurrentRoom = this;
            Guest.PlayerIndex = 1;
            return true;
        }

        public void RemovePlayer(ClientSession session)
        {
            if (Host == session)
            {
                Host = null;
            }
            else if (Guest == session)
            {
                Guest = null;
            }

            session.CurrentRoom = null;
            session.PlayerIndex = -1;
        }

        public ClientSession? GetOpponent(ClientSession session)
        {
            if (session == Host) return Guest;
            if (session == Guest) return Host;
            return null;
        }

        public async Task BroadcastAsync(string type, object? payload = null)
        {
            Task? t1 = Host?.SendAsync(type, payload);
            Task? t2 = Guest?.SendAsync(type, payload);

            if (t1 != null) await t1;
            if (t2 != null) await t2;
        }

        public async Task RelayToOpponentAsync(ClientSession sender, NetMessage msg)
        {
            ClientSession? opponent = GetOpponent(sender);
            if (opponent != null)
            {
                await opponent.SendRawMessageAsync(msg);
            }
        }

        public async Task CheckStartGameAsync()
        {
            if (Host != null && Guest != null && Host.Loadout != null && Guest.Loadout != null)
            {
                var startData = new GameStartData
                {
                    RoomId = RoomId,
                    HostLoadout = Host.Loadout,
                    GuestLoadout = Guest.Loadout,
                    RandomSeed = Random.Shared.Next(1, 1000000)
                };

                Console.WriteLine($"[Phòng {RoomId}] Cả 2 người đã sẵn sàng! Bắt đầu trận đấu...");
                await BroadcastAsync(NetMessageType.GameStart, startData);
            }
        }
    }
}
