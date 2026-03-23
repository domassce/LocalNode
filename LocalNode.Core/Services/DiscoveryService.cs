using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LocalNode.Core.Services;

public class DiscoveryService
{
    private const int DiscoveryPort = 9876;
    private const string Identifier = "LOCALNODE_V1";
    private CancellationTokenSource? _cts;
    public void StartAnnouncing(string nodeName, int port, bool requiresPassword)
    {
        _cts = new CancellationTokenSource();
        Task.Run(async () => {
            using var client = new UdpClient { EnableBroadcast = true };
            var data = Encoding.UTF8.GetBytes($"{Identifier}|{nodeName}|{port}|{requiresPassword}");

            while (!_cts.Token.IsCancellationRequested)
            {
                await client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                await Task.Delay(3000); 
            }
        });
    }
    public async Task ListenForNodes(Action<DiscoveredNode> onFound)
    {
        using var listener = new UdpClient(DiscoveryPort);
        while (true)
        {
            var result = await listener.ReceiveAsync();
            var msg = Encoding.UTF8.GetString(result.Buffer);

            if (msg.StartsWith(Identifier))
            {
                var parts = msg.Split('|');
                onFound(new DiscoveredNode(
                    Name: parts[1],
                    IP: result.RemoteEndPoint.Address.ToString(),
                    Port: int.Parse(parts[2]),
                    NeedsPassword: bool.Parse(parts[3])
                ));
            }
        }
    }

    public void Stop() => _cts?.Cancel();
}

public record DiscoveredNode(string Name, string IP, int Port, bool NeedsPassword);