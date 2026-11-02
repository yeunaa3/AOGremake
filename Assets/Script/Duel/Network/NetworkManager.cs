using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AOG.Duel.Network
{
    [DefaultExecutionOrder(-100)]
    public sealed class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Cài đặt kết nối Server")]
        [Tooltip("Địa chỉ WebSocket Server. Khi test 2 máy khác nhau trong cùng mạng Wifi/LAN, đổi localhost thành IP máy chủ (ví dụ: ws://192.168.1.15:8080/ws)")]
        [SerializeField] private string serverUrl = "ws://localhost:8080/ws";
        [SerializeField] private bool autoConnectOnStart = false;

        [Header("Thông tin phòng hiện tại (Read Only)")]
        [SerializeField] private string currentRoomId = string.Empty;
        [SerializeField] private int localPlayerIndex = -1; // 0 = Host (Player 1), 1 = Guest (Player 2)
        [SerializeField] private bool isConnected = false;

        public bool IsConnected => isConnected;
        public string CurrentRoomId => currentRoomId;
        public int LocalPlayerIndex => localPlayerIndex;
        public bool IsHost => localPlayerIndex == 0;

        // Các sự kiện mạng (chạy trên Main Thread của Unity)
        public event Action OnConnectedToServer;
        public event Action OnDisconnectedFromServer;
        public event Action<string, int> OnRoomCreated; // roomId, playerIndex (0)
        public event Action<string, int> OnRoomJoined;  // roomId, playerIndex (1)
        public event Action<int> OnOpponentJoined;       // opponent playerIndex
        public event Action<int> OnOpponentLeft;
        public event Action<GameStartData> OnGameStarted;
        public event Action<NetInputCmd> OnOpponentInputReceived;
        public event Action<string> OnErrorMessage;

        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;
        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (autoConnectOnStart)
            {
                await ConnectAsync();
            }
        }

        private void Update()
        {
            // Xử lý toàn bộ callback mạng an toàn trên Main Thread của Unity
            while (mainThreadQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkManager] Lỗi xử lý callback: {ex}");
                }
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public async Task<bool> ConnectAsync(string customUrl = null)
        {
            if (isConnected) return true;

            string targetUrl = !string.IsNullOrEmpty(customUrl) ? customUrl : serverUrl;
            Debug.Log($"[NetworkManager] Đang kết nối tới server: {targetUrl}...");

            try
            {
                Disconnect(); // Reset kết nối cũ nếu có

                cts = new CancellationTokenSource();
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(targetUrl), cts.Token);

                isConnected = true;
                Debug.Log("<color=green>[NetworkManager] Kết nối Server thành công!</color>");

                mainThreadQueue.Enqueue(() => OnConnectedToServer?.Invoke());

                _ = ReceiveLoopAsync(cts.Token);
                return true;
            }
            catch (Exception ex)
            {
                isConnected = false;
                Debug.LogError($"[NetworkManager] Kết nối Server thất bại: {ex.Message}");
                mainThreadQueue.Enqueue(() => OnErrorMessage?.Invoke($"Không thể kết nối đến server: {ex.Message}"));
                return false;
            }
        }

        public void Disconnect()
        {
            if (!isConnected && webSocket == null) return;

            isConnected = false;
            currentRoomId = string.Empty;
            localPlayerIndex = -1;

            try
            {
                cts?.Cancel();
                if (webSocket != null)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None).Wait(1000);
                    }
                    webSocket.Dispose();
                    webSocket = null;
                }
            }
            catch { }

            mainThreadQueue.Enqueue(() => OnDisconnectedFromServer?.Invoke());
        }

        public async Task CreateRoomAsync()
        {
            if (!isConnected)
            {
                bool connected = await ConnectAsync();
                if (!connected) return;
            }

            await SendMessageAsync(NetMessageType.CreateRoom, string.Empty);
        }

        public async Task JoinRoomAsync(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                OnErrorMessage?.Invoke("Mã phòng không được để trống!");
                return;
            }

            if (!isConnected)
            {
                bool connected = await ConnectAsync();
                if (!connected) return;
            }

            var req = new JoinRoomRequest { roomId = roomId.Trim() };
            string payload = JsonUtility.ToJson(req);
            await SendMessageAsync(NetMessageType.JoinRoom, payload);
        }

        public async Task SendReadyAsync(string bowId, string[] skillIds)
        {
            var readyData = new PlayerReadyData
            {
                bowId = bowId,
                skillIds = skillIds
            };
            string payload = JsonUtility.ToJson(readyData);
            await SendMessageAsync(NetMessageType.Ready, payload);
        }

        public async Task SendInputAsync(InputCmd cmd, long tick = 0)
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

            var netCmd = new NetInputCmd
            {
                playerIndex = localPlayerIndex,
                tick = tick,
                move = cmd.Move,
                dash = cmd.DashPressed,
                shield = cmd.ShieldPressed,
                skill1 = cmd.Skill1Pressed,
                skill2 = cmd.Skill2Pressed
            };

            string payload = JsonUtility.ToJson(netCmd);
            await SendMessageAsync(NetMessageType.PlayerInput, payload);
        }

        public async Task SendMessageAsync(string type, string payload)
        {
            if (webSocket == null || webSocket.State != WebSocketState.Open) return;

            var msg = new NetMessage(type, payload);
            string json = JsonUtility.ToJson(msg);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            await sendLock.WaitAsync();
            try
            {
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Lỗi gửi tin ({type}): {ex.Message}");
            }
            finally
            {
                sendLock.Release();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (webSocket != null && webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Disconnect();
                            return;
                        }
                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    string text = Encoding.UTF8.GetString(ms.ToArray());
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ProcessIncomingMessage(text);
                    }
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Debug.LogWarning($"[NetworkManager] Mất kết nối server: {ex.Message}");
                Disconnect();
            }
        }

        private void ProcessIncomingMessage(string json)
        {
            try
            {
                NetMessage msg = JsonUtility.FromJson<NetMessage>(json);
                if (msg == null) return;

                mainThreadQueue.Enqueue(() => HandleMessageOnMainThread(msg));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Lỗi đọc gói tin: {ex.Message}");
            }
        }

        private void HandleMessageOnMainThread(NetMessage msg)
        {
            switch (msg.type)
            {
                case NetMessageType.RoomCreated:
                    var createRes = JsonUtility.FromJson<CreateRoomResult>(msg.payload);
                    currentRoomId = createRes.roomId;
                    localPlayerIndex = createRes.playerIndex;
                    Debug.Log($"<color=cyan>[NetworkManager] Đã tạo phòng! Mã phòng: {currentRoomId} (Bạn là Host P1)</color>");
                    OnRoomCreated?.Invoke(currentRoomId, localPlayerIndex);
                    break;

                case NetMessageType.RoomJoined:
                    var joinRes = JsonUtility.FromJson<JoinRoomResult>(msg.payload);
                    currentRoomId = joinRes.roomId;
                    localPlayerIndex = joinRes.playerIndex;
                    Debug.Log($"<color=cyan>[NetworkManager] Đã vào phòng {currentRoomId}! (Bạn là Guest P2)</color>");
                    OnRoomJoined?.Invoke(currentRoomId, localPlayerIndex);
                    break;

                case NetMessageType.PlayerJoined:
                    Debug.Log("<color=yellow>[NetworkManager] Đối thủ đã vào phòng!</color>");
                    OnOpponentJoined?.Invoke(1);
                    break;

                case NetMessageType.PlayerLeft:
                    Debug.Log("<color=orange>[NetworkManager] Đối thủ đã rời phòng.</color>");
                    OnOpponentLeft?.Invoke(1);
                    break;

                case NetMessageType.GameStart:
                    var startData = JsonUtility.FromJson<GameStartData>(msg.payload);
                    Debug.Log("<color=green>[NetworkManager] Cả 2 đã sẵn sàng! Bắt đầu trận đấu!</color>");
                    OnGameStarted?.Invoke(startData);
                    break;

                case NetMessageType.PlayerInput:
                    var inputCmd = JsonUtility.FromJson<NetInputCmd>(msg.payload);
                    OnOpponentInputReceived?.Invoke(inputCmd);
                    break;

                case NetMessageType.Error:
                    Debug.LogError($"[NetworkManager] Lỗi từ Server: {msg.payload}");
                    OnErrorMessage?.Invoke(msg.payload);
                    break;
            }
        }
    }
}
