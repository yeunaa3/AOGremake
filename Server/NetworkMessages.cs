using System;
using System.Text.Json.Serialization;

namespace AOG.Server
{
    public static class NetMessageType
    {
        // Hệ thống phòng & sảnh
        public const string CreateRoom = "CreateRoom";
        public const string RoomCreated = "RoomCreated";
        public const string JoinRoom = "JoinRoom";
        public const string RoomJoined = "RoomJoined";
        public const string PlayerJoined = "PlayerJoined";
        public const string PlayerLeft = "PlayerLeft";
        public const string Error = "Error";

        // Đồng bộ trận đấu
        public const string Ready = "Ready";
        public const string GameStart = "GameStart";
        public const string PlayerInput = "PlayerInput";
        public const string SyncHealth = "SyncHealth";
        public const string GameOver = "GameOver";
    }

    /// <summary>
    /// Cấu trúc gói tin mạng chuẩn gửi qua lại giữa Unity và Server.
    /// </summary>
    public sealed class NetMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public string Payload { get; set; } = string.Empty;

        public NetMessage() { }

        public NetMessage(string type, string payload = "")
        {
            Type = type;
            Payload = payload;
        }
    }

    // Các DTO Payload cụ thể:

    public sealed class CreateRoomResult
    {
        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("playerIndex")]
        public int PlayerIndex { get; set; } = 0; // 0 = Host (Player 1)
    }

    public sealed class JoinRoomRequest
    {
        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;
    }

    public sealed class JoinRoomResult
    {
        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("playerIndex")]
        public int PlayerIndex { get; set; } = 1; // 1 = Guest (Player 2)
    }

    public sealed class PlayerReadyData
    {
        [JsonPropertyName("bowId")]
        public string BowId { get; set; } = string.Empty;

        [JsonPropertyName("skillIds")]
        public string[] SkillIds { get; set; } = Array.Empty<string>();
    }

    public sealed class GameStartData
    {
        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("hostLoadout")]
        public PlayerReadyData? HostLoadout { get; set; }

        [JsonPropertyName("guestLoadout")]
        public PlayerReadyData? GuestLoadout { get; set; }

        [JsonPropertyName("randomSeed")]
        public int RandomSeed { get; set; }
    }

    /// <summary>
    /// Gói tin đồng bộ phím bấm điều khiển từ client lên server và chuyển tiếp sang đối thủ.
    /// Khớp với struct InputCmd trong Unity!
    /// </summary>
    public sealed class NetInputCmd
    {
        [JsonPropertyName("playerIndex")]
        public int PlayerIndex { get; set; }

        [JsonPropertyName("tick")]
        public long Tick { get; set; }

        [JsonPropertyName("move")]
        public float Move { get; set; }

        [JsonPropertyName("dash")]
        public bool DashPressed { get; set; }

        [JsonPropertyName("shield")]
        public bool ShieldPressed { get; set; }

        [JsonPropertyName("skill1")]
        public bool Skill1Pressed { get; set; }

        [JsonPropertyName("skill2")]
        public bool Skill2Pressed { get; set; }
    }
}
