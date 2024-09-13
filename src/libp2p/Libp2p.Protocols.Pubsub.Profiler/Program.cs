// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT


using Libp2p.Protocols.PubSubDiscovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Discovery;
using Nethermind.Libp2p.Core.TestsBase;
using Nethermind.Libp2p.Core.TestsBase.E2e;
using Nethermind.Libp2p.Protocols.Pubsub;
using System.Text;

int totalCount = 5;
TestContextLoggerFactory fac = new TestContextLoggerFactory();
// There is common communication point
ChannelBus commonBus = new(fac);
ILocalPeer[] peers = new ILocalPeer[totalCount];
PeerStore[] peerStores = new PeerStore[totalCount];
PubsubRouter[] routers = new PubsubRouter[totalCount];


for (int i = 0; i < totalCount; i++)
{
    // But we create a seprate setup for every peer
    ServiceProvider sp = new ServiceCollection()
           .AddSingleton(sp => new TestBuilder(commonBus, sp).AddAppLayerProtocol<GossipsubProtocol>())
           .AddSingleton<ILoggerFactory>(sp => fac)
           .AddSingleton<PubsubRouter>()
           .AddSingleton<PeerStore>()
           .AddSingleton(sp => sp.GetService<IPeerFactoryBuilder>()!.Build())
           .BuildServiceProvider();

    IPeerFactory peerFactory = sp.GetService<IPeerFactory>()!;
    ILocalPeer peer = peers[i] = peerFactory.Create(TestPeers.Identity(i));
    PubsubRouter router = routers[i] = sp.GetService<PubsubRouter>()!;
    PeerStore peerStore = sp.GetService<PeerStore>()!;
    PubSubDiscoveryProtocol disc = new(router, new PubSubDiscoverySettings() { Interval = 300 }, peerStore, peer);
    _ = router.RunAsync(peer, peerStore);
    peerStores[i] = peerStore;
    _ = disc.DiscoverAsync(peers[i].Address);
}

for (int i = 0; i < peers.Length; i++)
{
    peerStores[i].Discover([peers[(i + 1) % totalCount].Address]);
}

await Task.Delay(10000);

Console.WriteLine("Routers");

for (int i = 0; i < routers.Length; i++)
{
    Console.WriteLine(routers[i].ToString());
}

Console.WriteLine("Stores");

for (int i = 0; i < peerStores.Length; i++)
{
    Console.WriteLine(peerStores[i].ToString());
}

for (int i = 0; i < routers.Length; i++)
{
    routers[i].Subscribe("test");
}

await Task.Delay(5000);

var testTopic = routers[1].Subscribe("test");
var testTopicEnd = routers[totalCount - 1].Subscribe("test");
testTopicEnd.OnMessage += (s) => Console.WriteLine(Encoding.UTF8.GetString(s));

testTopic.Publish(Encoding.UTF8.GetBytes("test"));

for (int i = 0; i < 20; i++)
{
    Console.WriteLine(i * 100);
    await Task.Delay(100);
}

Console.WriteLine("Routers");

for (int i = 0; i < routers.Length; i++)
{
    Console.WriteLine(routers[i].ToString());
}