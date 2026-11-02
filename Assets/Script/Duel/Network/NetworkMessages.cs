using System;

namespace AOG.Duel.Network
{
    public static class NetMessageType
    {
        public const string CreateRoom = "CreateRoom";
        public const string RoomCreated = "RoomCreated";
        public const string JoinRoom = "JoinRoom";
        public const string RoomJoined = "RoomJoined";
        public const string PlayerJoined = "PlayerJoined";
        public const string PlayerLeft = "PlayerLeft";
        public const string Error = "Error";

        public const string Ready = "Ready";
        public const string GameStart = "GameStart";
        public const string PlayerInput = "PlayerInput";
        public const string SyncHealth = "SyncHealth";
        public const string GameOver = "GameOver";
    }

    [Serializable]
    public sealed class NetMessage
    {
        public string type;
        public string payload;

        public NetMessage() { }
        public NetMessage(string type, string payload = "")
        {
            this.type = type;
            this.payload = payload;
        }
    }

    [Serializable]
    public sealed class CreateRoomResult
    {
        public string roomId;
        public int playerIndex;
    }

    [Serializable]
    public sealed class JoinRoomRequest
    {
        public string roomId;
    }

    [Serializable]
    public sealed class JoinRoomResult
    {
        public string roomId;
        public int playerIndex;
    }

    [Serializable]
    public sealed class PlayerReadyData
    {
        public string bowId;
        public string[] skillIds;
    }

    [Serializable]
    public sealed class GameStartData
    {
        public string roomId;
        public PlayerReadyData hostLoadout;
        public PlayerReadyData guestLoadout;
        public int randomSeed;
    }

    [Serializable]
    public struct NetInputCmd
    {
        public int playerIndex;
        public long tick;
        public float move;
        public bool dash;
        public bool shield;
        public bool skill1;
        public bool skill2;
    }
}
