// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Discovery;
using Nethermind.Libp2p.Core.TestsBase;
using Nethermind.Libp2p.Core.TestsBase.E2e;
using Nethermind.Libp2p.Protocols;
using Nethermind.Libp2p.Protocols.Pubsub;
using Nethermind.Libp2p.Stack;
using System.Text;

int totalCount = 20;
TestContextLoggerFactory fac = new();
// There is common communication point
ChannelBus commonBus = new(fac);
ILocalPeer[] peers = new ILocalPeer[totalCount];
PeerStore[] peerStores = new PeerStore[totalCount];
PubsubRouter[] routers = new PubsubRouter[totalCount];


for (int i = 0; i < totalCount; i++)
{
    // But we create a seprate setup for every peer
    ServiceProvider sp = new ServiceCollection()
            .AddLibp2p(sp => sp)
            .AddSingleton(sp => new TestBuilder(commonBus, sp).AddAppLayerProtocol<GossipsubProtocol>())
            // .AddSingleton<ILoggerFactory>(sp => fac)
            .AddSingleton<PubsubRouter>()
            .AddSingleton<PeerStore>()
            .AddSingleton(sp => sp.GetService<IPeerFactoryBuilder>()!.Build())
            // .AddSingleton<ILoggerFactory>(new NethermindLoggerFactory(logManager, overrideAllWith: Microsoft.Extensions.Logging.LogLevel.Information))
            .AddLogging(builder =>
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                .AddSimpleConsole(l =>
                {
                    l.SingleLine = true;
                    l.TimestampFormat = "[HH:mm:ss.FFF]";
                })
            )
           .BuildServiceProvider();

    IPeerFactory peerFactory = sp.GetService<IPeerFactory>()!;
    ILocalPeer peer = peers[i] = peerFactory.Create(TestPeers.Identity(i));
    PubsubRouter router = routers[i] = sp.GetService<PubsubRouter>()!;
    PubsubPeerDiscoveryProtocol disc = new(router, peerStores[i] = sp.GetService<PeerStore>()!, new PubsubPeerDiscoverySettings() { Interval = 300 }, peer);

    await peer.ListenAsync(TestPeers.Multiaddr(i));
    _ = router.RunAsync(peer);
    _ = disc.DiscoverAsync(peer.Address);
}

for (int i = 0; i < peers.Length; i++)
{
    peerStores[i].Discover([peers[(i + 1) % totalCount].Address]);
}

// await Task.Delay(10000);

// Console.WriteLine("Routers");

// for (int i = 0; i < routers.Length; i++)
// {
//     Console.WriteLine(routers[i].ToString());
// }

// Console.WriteLine("Stores");

// for (int i = 0; i < peerStores.Length; i++)
// {
//     Console.WriteLine(peerStores[i].ToString());
// }

// for (int i = 0; i < routers.Length; i++)
// {
//     routers[i].GetTopic("test");
// }

await Task.Delay(5000);

var testTopic = routers[1].GetTopic("test");
// var testTopicEnd = routers[totalCount - 1].GetTopic("test");
// testTopicEnd.OnMessage += (s) => Console.WriteLine(Encoding.UTF8.GetString(s));

int recvCount = 0;
for (int i = 0; i < routers.Length; i++)
{
    int index = i;
    Console.WriteLine("Subscribing " + index);
    routers[index].GetTopic("test").OnMessage += (s) => {
        Console.WriteLine(index + " recv: " + Encoding.UTF8.GetString(s));
        Interlocked.Increment(ref recvCount);
    };
}

await Task.Delay(10000);

for (int iter = 0;; iter++)
{
    Console.WriteLine("Publishing " + iter);
    testTopic.Publish(Encoding.UTF8.GetBytes("test" + iter));
    await Task.Delay(5000);
    Console.WriteLine("count: " + recvCount);
    Interlocked.Exchange(ref recvCount, 0);
    Console.WriteLine("---");
}

// for (int i = 0; i < 20; i++)
// {
//     Console.WriteLine(i * 100);
//     await Task.Delay(100);
// }

// Console.WriteLine("Routers");

// for (int i = 0; i < routers.Length; i++)
// {
//     Console.WriteLine(routers[i].ToString());
// }
