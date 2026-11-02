using UnityEngine;

namespace AOG.Duel.Network
{
    /// <summary>
    /// Giao diện OnGUI đơn giản để test kết nối Server, Tạo phòng, Vào phòng ngay trong Unity Game View.
    /// </summary>
    public sealed class NetworkLobbyTestUI : MonoBehaviour
    {
        [SerializeField] private string targetIp = "localhost";
        [SerializeField] private string roomInput = "";

        private string statusLog = "Chưa kết nối Server.";

        private void Start()
        {
            var net = NetworkManager.Instance;
            if (net == null)
            {
                var go = new GameObject("NetworkManager");
                go.AddComponent<NetworkManager>();
            }

            NetworkManager.Instance.OnConnectedToServer += () => statusLog = "Đã kết nối Server thành công!";
            NetworkManager.Instance.OnDisconnectedFromServer += () => statusLog = "Đã ngắt kết nối Server.";
            NetworkManager.Instance.OnRoomCreated += (id, pIdx) => statusLog = $"Đã tạo phòng: {id}. Đang đợi người chơi 2...";
            NetworkManager.Instance.OnRoomJoined += (id, pIdx) => statusLog = $"Đã vào phòng: {id}. Bạn là Player 2!";
            NetworkManager.Instance.OnOpponentJoined += (pIdx) => statusLog = "Đối thủ đã vào phòng! Sẵn sàng chiến đấu!";
            NetworkManager.Instance.OnOpponentLeft += (pIdx) => statusLog = "Đối thủ đã thoát phòng.";
            NetworkManager.Instance.OnGameStarted += (data) => statusLog = $"TRẬN ĐẤU BẮT ĐẦU! (Seed: {data.randomSeed})";
            NetworkManager.Instance.OnErrorMessage += (err) => statusLog = $"<color=red>Lỗi: {err}</color>";
        }

        private void OnGUI()
        {
            var net = NetworkManager.Instance;
            if (net == null) return;

            GUI.Box(new Rect(10, 10, 320, 260), "AOG Realtime Network Test");

            GUI.Label(new Rect(20, 35, 300, 25), $"Trạng thái: {statusLog}");

            if (!net.IsConnected)
            {
                GUI.Label(new Rect(20, 70, 70, 20), "Server IP:");
                targetIp = GUI.TextField(new Rect(95, 70, 140, 22), targetIp);

                if (GUI.Button(new Rect(240, 70, 80, 22), "Kết nối"))
                {
                    statusLog = "Đang kết nối...";
                    _ = net.ConnectAsync($"ws://{targetIp}:8080/ws");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(net.CurrentRoomId))
                {
                    if (GUI.Button(new Rect(20, 75, 140, 35), "Tạo Phòng Mới"))
                    {
                        statusLog = "Đang tạo phòng...";
                        _ = net.CreateRoomAsync();
                    }

                    GUI.Label(new Rect(20, 125, 70, 20), "Mã phòng:");
                    roomInput = GUI.TextField(new Rect(95, 125, 100, 22), roomInput);

                    if (GUI.Button(new Rect(200, 125, 90, 22), "Vào Phòng"))
                    {
                        statusLog = $"Đang vào phòng {roomInput}...";
                        _ = net.JoinRoomAsync(roomInput);
                    }
                }
                else
                {
                    GUI.Label(new Rect(20, 75, 280, 25), $"Mã Phòng: <b>{net.CurrentRoomId}</b> | Vai trò: {(net.IsHost ? "Player 1 (Host)" : "Player 2 (Guest)")}");

                    if (GUI.Button(new Rect(20, 110, 150, 35), "Bấm Sẵn Sàng (Ready)"))
                    {
                        statusLog = "Đã bấm sẵn sàng, chờ đối thủ...";
                        _ = net.SendReadyAsync("default_bow", new string[] { "skill_1", "skill_2" });
                    }
                }

                if (GUI.Button(new Rect(20, 220, 100, 25), "Ngắt kết nối"))
                {
                    net.Disconnect();
                }
            }
        }
    }
}
