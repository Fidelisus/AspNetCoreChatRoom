using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreChatRoom
{
    public class ChatWebSocketMiddleware
    {
        public const int BUFFER_SIZE = 4016;
        private static ConcurrentDictionary<KeyValuePair<string, string>, WebSocket> _sockets = new ConcurrentDictionary<KeyValuePair<string, string>, WebSocket>();
        public static string userName { get; set; }

        private readonly RequestDelegate _next;

        public ChatWebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }
            
            CancellationToken ct = context.RequestAborted;
            WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();
            var socketId = Guid.NewGuid().ToString();

            _sockets.TryAdd(new KeyValuePair<string, string>(socketId, userName), currentSocket);

            while (true)
            {
                if (ct.IsCancellationRequested){  break;  }

                var response = await ReceiveStringAsync(currentSocket, ct);
                if(string.IsNullOrEmpty(response))
                {
                    if(currentSocket.State != WebSocketState.Open)
                    {
                        break;
                    }

                    continue;
                }

                foreach (var socket in _sockets)
                {
                    if(socket.Value.State != WebSocketState.Open)
                    {
                        continue;
                    }

                        if (response.Contains("show"))
                        {
                            foreach (var s in _sockets)
                            {
                                SendStringAsync(socket.Value, s.ToString(), ct).Wait();
                            }
                        }

                        await SendStringAsync(socket.Value, response, ct);
                }
            }

            await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
        }

        private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

        private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
        {
            var buffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);
            using (var message = new MemoryStream())
            {
                WebSocketReceiveResult result;
                

                    ct.ThrowIfCancellationRequested();
                    result = await socket.ReceiveAsync(buffer, ct);
                    message.Write(buffer.Array, buffer.Offset, result.Count);

                if (result.MessageType != WebSocketMessageType.Text){   return null;    }

                message.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(message, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
