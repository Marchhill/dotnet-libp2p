// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

using Google.Protobuf;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nethermind.Libp2p.Core;

public interface IReader
{
    /// <summary>
    /// Waits for any data or EOF marker, returns <c>false</c> if EOF was reached, <c>true</c> without reading data otherwise.
    /// </summary>
    /// <returns></returns>
    ValueTask<bool> CanReadAsync(CancellationToken token = default);

    ValueTask<ReadOnlySequence<byte>> ReadAsync(int length, ReadBlockingMode blockingMode = ReadBlockingMode.WaitAll,
        CancellationToken token = default);


    #region Read helpers
    async Task<string> ReadLineAsync()
    {
        int size = await ReadVarintAsync();
        return Encoding.UTF8.GetString((await ReadAsync(size)).ToArray()).TrimEnd('\n');
    }

    Task<int> ReadVarintAsync(CancellationToken token = default)
    {
        return VarInt.Decode(this, token);
    }

    Task<ulong> ReadVarintUlongAsync()
    {
        return VarInt.DecodeUlong(this);
    }

    async ValueTask<T> ReadPrefixedProtobufAsync<T>(MessageParser<T> parser, CancellationToken token = default) where T : IMessage<T>
    {
        int messageLength = await ReadVarintAsync(token);
        ReadOnlySequence<byte> serializedMessage = await ReadAsync(messageLength, token: token);

        return parser.ParseFrom(serializedMessage);
    }

    async IAsyncEnumerable<ReadOnlySequence<byte>> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (!token.IsCancellationRequested && await CanReadAsync())
        {
            yield return await ReadAsync(0, ReadBlockingMode.WaitAny, token);
        }
    }

    #endregion
}

public enum ReadBlockingMode
{
    WaitAll,
    WaitAny,
    DontWait
}
