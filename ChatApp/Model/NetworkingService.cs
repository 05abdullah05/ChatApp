using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace ChatApp.Model
{
    public class NetworkingService
    {
        private TcpListener? _listener;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public event Action<string>? OnStatus;
        public event Action<string>? OnInviteReceived;
        //public event Action? OnInviteAccepted;
        public event Action<string>? OnInviteAccepted;
        public event Action? OnInviteRejected;
        public event Action? OnDisconnected;
        public event Action<string, string>? OnMessageReceived;  // First string is sender, second is message
        public event Action? OnBuzzReceived;                     // New event for buzz notifications




        private void Status(string msg) => OnStatus?.Invoke(msg);

        public async Task StartListeningAsync(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();

                Status($"[LISTENING] Waiting on port {port}...");

                _client = await _listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();

                Status("[CONNECTED] Client connected.");

                // begin receiving JSON packets
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                Status($"[ERROR] Listener failed: {ex.Message}");
            }
        }

        // ---------------- CONNECT TO PEER ----------------
        public async Task ConnectAsync(string ip, int port)
        {
            try
            {
                Status($"[CONNECT] Attempting connection to {ip}:{port}...");

                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Parse(ip), port);
                _stream = _client.GetStream();

                Status("[CONNECTED] Connected to peer.");

                // begin receiving messages
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                Status($"[ERROR] Connect failed: {ex.Message}");
            }
        }

        // ---------------- SEND JSON ----------------
        public async Task SendJsonAsync(object obj)
        {
            if (_stream == null)
            {
                Status("[ERROR] Friend is not online, try another portnumber");
                return;
            }

            string json = JsonSerializer.Serialize(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");

            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }

        // ---------------- RECEIVE LOOP ----------------
        private async Task ReceiveLoopAsync()
        {
            try
            {
                byte[] buffer = new byte[4096];

                while (true)
                {
                    int bytes = await _stream!.ReadAsync(buffer, 0, buffer.Length);
                    if (bytes == 0)
                    {
                        Status("[DISCONNECTED] Peer closed connection.");
                        ResetClient();
                        OnDisconnected?.Invoke();
                        ResumeListening();   // 🔥 THIS IS THE KEY
                        return;
                    }


                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    HandleIncoming(msg);
                }
            }
            catch
            {
                Status("[DISCONNECTED] Connection lost.");
                OnDisconnected?.Invoke();
            }
        }

        // ---------------- HANDLE INCOMING JSON ----------------
        private void HandleIncoming(string raw)
        {
            string[] messages = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var json in messages)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string type = root.GetProperty("type").GetString()!;

                switch (type)
                {
                    case "invite":  // New case for incoming invites
                        string name = root.GetProperty("name").GetString()!;
                        Status($"[INVITE] Received invite from {name}");
                        OnInviteReceived?.Invoke(name);
                        break;

                    case "accept":  // New case for invite acceptance
                        string acceptorName = root.GetProperty("name").GetString()!;
                        Status($"{acceptorName} accepted the invite.");
                        OnInviteAccepted?.Invoke(acceptorName);
                        break;
                    
                    case "reject":
                        Status("[INVITE] Your invite was rejected.");
                        ResetClient();          // close this connection only
                        OnInviteRejected?.Invoke();
                        break;


                    case "message": // New case for incoming messages
                        string sender = root.GetProperty("sender").GetString()!;
                        string text = root.GetProperty("text").GetString()!;
                        OnMessageReceived?.Invoke(sender, text);
                        break;
                    case "buzz":    // New case for buzz notifications
                        Status("[BUZZ] Buzz received!");
                        OnBuzzReceived?.Invoke();
                        break;




                    default:
                        Status($"[UNKNOWN] {json}");
                        break;
                }
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                _listener?.Stop();
            }
            catch { }   
        }

        public async Task SendMessageAsync(string sender, string text)
        {
            await SendJsonAsync(new
            {
                type = "message",
                sender = sender,
                text = text
            });
        }

        private void ResetClient()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }

            _stream = null;
            _client = null;
        }
        private async void ResumeListening()
        {
            if (_listener == null)
                return;

            try
            {
                Status("[LISTENING] Waiting for new invite...");
                _client = await _listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                _ = ReceiveLoopAsync();
            }
            catch { }
        }
        public async Task SendBuzzAsync()
        {
            await SendJsonAsync(new
            {
                type = "buzz"
            });
        }


    }
}

