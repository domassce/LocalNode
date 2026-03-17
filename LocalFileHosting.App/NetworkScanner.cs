using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace LocalFileHosting.App
{
    public class DiscoveredHost
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
    }

    public class NetworkScanner(string hostName, int hostPort) : IDisposable
    {
        private readonly int _broadcastPort = 5051;
        private readonly UdpClient _udpClient = new();
        private CancellationTokenSource? _cts;
        private readonly Dictionary<string, DiscoveredHost> _hosts = [];
        private readonly string _hostName = hostName;
        private readonly int _hostPort = hostPort;
        public void StartBroadcasting()
        {

            _cts = new CancellationTokenSource();
            _udpClient.EnableBroadcast = true;

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var message = $"FILEHOST:{_hostName}:{_hostPort}";
                        var bytes = Encoding.UTF8.GetBytes(message);
                        var endpoint = new IPEndPoint(IPAddress.Broadcast, _broadcastPort);
                        await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
                        await Task.Delay(2000, _cts.Token);
                    }
                    catch (TaskCanceledException) { break; }
                    catch { /* Ignore broadcast errors */ }
                }
            }, _cts.Token);
        }

        public void StartListening()
        {
            _cts = new CancellationTokenSource();
            
            try 
            {
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _broadcastPort));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Could not bind UDP port for network discovery. Auto-discovery may not work. ({ex.Message})[/]");
                return;
            }

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udpClient.ReceiveAsync();
                        var message = Encoding.UTF8.GetString(result.Buffer);
                        
                        if (message.StartsWith("FILEHOST:"))
                        {
                            var parts = message.Split(':');
                            if (parts.Length == 3)
                            {
                                var name = parts[1];
                                var port = parts[2];
                                var ip = result.RemoteEndPoint.Address.ToString();
                                var url = $"http://{ip}:{port}";
                                
                                lock (_hosts)
                                {
                                    _hosts[url] = new DiscoveredHost
                                    {
                                        Name = name,
                                        Url = url,
                                        LastSeen = DateTime.Now
                                    };
                                }
                            }
                        }
                    }
                    catch (ObjectDisposedException) { break; }
                    catch { /* Ignore receive errors */ }
                }
            });
        }

        public List<DiscoveredHost> GetActiveHosts()
        {
            lock (_hosts)
            {
                var active = new List<DiscoveredHost>();
                var now = DateTime.Now;
                foreach (var host in _hosts.Values)
                {
                    if ((now - host.LastSeen).TotalSeconds < 10)
                    {
                        active.Add(host);
                    }
                }
                return active;
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _udpClient?.Close();
            _udpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
