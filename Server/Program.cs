using System;
using System.Net.WebSockets;
using AOG.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Lắng nghe trên tất cả card mạng (0.0.0.0) tại cổng 8080 để máy khác trong LAN/Wifi cũng kết nối được!
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

var roomManager = new RoomManager();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var session = new ClientSession(webSocket);
        Console.WriteLine($"[Kết nối mới] Client {session.SessionId} từ {context.Connection.RemoteIpAddress}");

        await session.StartReceiveLoopAsync(
            onMessageReceived: async (s, msg) => await roomManager.HandleMessageAsync(s, msg),
            onDisconnected: s => roomManager.OnClientDisconnected(s)
        );
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapGet("/", () => "AOG Realtime Duel Game Server is running!");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("=================================================");
Console.WriteLine("       AOG 1v1 REALTIME GAME SERVER (.NET)       ");
Console.WriteLine("=================================================");
Console.ResetColor();
Console.WriteLine(">> Dang lang nghe tai: ws://localhost:8080/ws");
Console.WriteLine(">> Hoac ws://<IP_May_Tinh>:8080/ws (cho may khac trong LAN/Wifi)");
Console.WriteLine(">> Nhan Ctrl+C de dung server.\n");

app.Run();
