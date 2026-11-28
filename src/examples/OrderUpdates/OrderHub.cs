// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

namespace OrderUpdates;

/// <summary>Provides order status lookups, callbacks and a status history.</summary>
public sealed class OrderHub : Hub
{
    /// <summary>Gets the current status of an order.</summary>
    /// <param name="number">The order number.</param>
    /// <returns>The order number and status.</returns>
    public string Status(int number)
    {
        Context.ConnectionAborted.ThrowIfCancellationRequested();
        return $"{number} shipped";
    }

    /// <summary>Sends an order update to the subscribed client.</summary>
    /// <param name="number">The order number.</param>
    /// <returns>Completion when the update has been sent.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Notify(int number) => Clients.Caller.SendAsync("OrderChanged", number, "shipped", Context.ConnectionAborted);

    /// <summary>Streams the status changes for an order in chronological order.</summary>
    /// <param name="number">The order number.</param>
    /// <param name="cancellationToken">Cancels the history stream.</param>
    /// <returns>The order's status changes.</returns>
    public async IAsyncEnumerable<string> History(int number, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return $"{number} received";
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return Status(number);
    }
}
