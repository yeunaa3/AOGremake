using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AOG.Server
{
    public sealed class ClientSession
    {
        public string SessionId { get; } = Guid.NewGuid().ToString("N")[..8];
        public WebSocket Socket { get; }
        public GameRoom? CurrentRoom { get; set; }
        public int PlayerIndex { get; set; } = -1; // 0 = Host, 1 = Guest
        public PlayerReadyData? Loadout { get; set; }

        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private readonly byte[] receiveBuffer = new byte[4096];

        public ClientSession(WebSocket socket)
        {
            Socket = socket;
        }

        public async Task StartReceiveLoopAsync(Func<ClientSession, NetMessage, Task> onMessageReceived, Action<ClientSession> onDisconnected)
        {
            try
            {
                while (Socket.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await Socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseAsync();
                            onDisconnected(this);
                            return;
                        }
                        ms.Write(receiveBuffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    string text = Encoding.UTF8.GetString(ms.ToArray());
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        try
                        {
                            NetMessage? msg = JsonSerializer.Deserialize<NetMessage>(text);
                            if (msg != null)
                            {
                                await onMessageReceived(this, msg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{SessionId}] Lỗi parse JSON: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{SessionId}] Ngắt kết nối ({ex.Message})");
            }
            finally
            {
                onDisconnected(this);
            }
        }

        public async Task SendAsync(string type, object? payloadObj = null)
        {
            if (Socket.State != WebSocketState.Open) return;

            string payloadJson = payloadObj != null ? JsonSerializer.Serialize(payloadObj) : string.Empty;
            NetMessage msg = new NetMessage(type, payloadJson);
            string json = JsonSerializer.Serialize(msg);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            await sendLock.WaitAsync();
            try
            {
                if (Socket.State == WebSocketState.Open)
                {
                    await Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async Task SendRawMessageAsync(NetMessage msg)
        {
            if (Socket.State != WebSocketState.Open) return;

            string json = JsonSerializer.Serialize(msg);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            await sendLock.WaitAsync();
            try
            {
                if (Socket.State == WebSocketState.Open)
                {
                    await Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            try
            {
                if (Socket.State == WebSocketState.Open || Socket.State == WebSocketState.CloseReceived)
                {
                    await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch { }
        }
    }
}
