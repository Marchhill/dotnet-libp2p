using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Multiformats.Address;

using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Discovery;
using Nethermind.Libp2p.Protocols.Pubsub;
using Nethermind.Libp2p.Stack;
using NReco.Logging.File;
using Nethermind.Libp2p.Protocols;

using Nethermind.Libp2p.Core.Dto;
using Google.Protobuf;

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLibp2p(builder => builder)
    .AddSingleton(new IdentifyProtocolSettings
    {
        ProtocolVersion = "/shutter/0.1.0",
        AgentVersion = "github.com/shutter-network/rolling-shutter/rolling-shutter"
    })
    // .AddLogging(builder =>
    //         builder.SetMinimumLevel(LogLevel.Trace)
    //         .AddFile("/home/marc/shutter.log", append: true)
    //         .AddSimpleConsole(l =>
    //         {
    //             l.SingleLine = true;
    //             l.TimestampFormat = "[HH:mm:ss.FFF]";
    //         }))
    .BuildServiceProvider();

IPeerFactory peerFactory = serviceProvider.GetService<IPeerFactory>()!;

// ILogger logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger("Pubsub Chat");
CancellationTokenSource ts = new();

ILocalPeer peer = peerFactory.Create(new Identity(), "/ip4/0.0.0.0/tcp/23102");

Console.WriteLine(peer.Address);

PubsubRouter router = serviceProvider.GetService<PubsubRouter>()!;

ITopic topic = router.Subscribe("decryptionKeys");
topic.OnMessage += (byte[] msg) =>
{
    Envelope envelope = Envelope.Parser.ParseFrom(msg);
    DecryptionKeys decryptionKeys = DecryptionKeys.Parser.ParseFrom(envelope.Message.ToByteString());
    Console.WriteLine(decryptionKeys.Eon);
    Console.WriteLine(decryptionKeys.InstanceID);
};

MyProto proto = new();

_ = router.RunAsync(peer, proto, token: ts.Token);

// Add Peers
proto.OnAddPeer?.Invoke(["/ip4/64.226.117.95/tcp/23000/p2p/12D3KooWDu1DQcEXyJRwbq6spG5gbi11MbN3iSSqbc2Z85z7a8jB"]);
proto.OnAddPeer?.Invoke(["/ip4/64.226.117.95/tcp/23001/p2p/12D3KooWFbscPyxc3rxyoEgyLbDYpbfx6s6di5wnr4cFz77q3taH"]);
proto.OnAddPeer?.Invoke(["/ip4/64.226.117.95/tcp/23002/p2p/12D3KooWLmDDaCkXZgkWUnWZ1RxLzA1FHm4cVHLnNvCuGi4haGLu"]);
proto.OnAddPeer?.Invoke(["/ip4/64.226.117.95/tcp/23003/p2p/12D3KooW9y8s8gy52jHXvJXNU5D2HuDmXxrs5Kp4VznbiBtRUnU5"]);

Console.ReadLine();
Console.WriteLine("Finished");

internal class MyProto : IDiscoveryProtocol
{
    public Func<Multiaddress[], bool>? OnAddPeer { get; set; }
    public Func<Multiaddress[], bool>? OnRemovePeer { get; set; }

    public Task DiscoverAsync(Multiaddress localPeerAddr, CancellationToken token = default)
    {
        return Task.Delay(int.MaxValue);
    }
}
