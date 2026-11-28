// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace OrderUpdates;

/// <summary>Reads an order, receives a server callback and streams its history.</summary>
internal static class Program
{
    /// <summary>Runs a local hub and checks the messages sent over its HTTP transport.</summary>
    /// <returns>Completion after the client has stopped and the server has shut down.</returns>
    /// <exception cref="InvalidOperationException">The server sends unexpected order data or the connection closes with an error.</exception>
    private static async Task Main()
    {
        const int OrderNumber = 42;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var token = deadline.Token;
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        _ = builder.Services.AddSignalR();
        await using var app = builder.Build();
        var interruptNextPoll = 0;
        _ = app.Use(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.Request.Method) && Interlocked.Exchange(ref interruptNextPoll, 0) != 0)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }

            await next(context);
        });
        _ = app.MapHub<OrderHub>("/orders");
        await app.StartAsync(token);
        var server = app.GetTestServer();
        var ready = new TaskCompletionSource<HubConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lifetime = HubBuilder.Create(hub => hub.WithUrl(new Uri(server.BaseAddress, "/orders"), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => server.CreateHandler();
        }).WithAutomaticReconnect([TimeSpan.Zero])).Subscribe(
            pair => ready.TrySetResult(pair.HubConnection),
            error => ready.TrySetException(error));
        var connection = await ready.Task.WaitAsync(token);
        _ = await connection.StartObservable(1, token).ToTask(token);
        _ = await connection.EnsureStarted(token).ToTask(token);
        await CheckOrderAsync(connection, OrderNumber, token);

        // Finish the current long poll with a callback so the next poll reaches the simulated outage.
        var reconnecting = connection.IsReconnecting().Take(1).ToTask(token);
        var reconnected = connection.HasReconnected().Take(1).ToTask(token);
        _ = Interlocked.Exchange(ref interruptNextPoll, 1);
        _ = await connection.SendObservable(nameof(OrderHub.Notify), token, OrderNumber).ToTask(token);
        _ = await reconnecting;
        var replacement = await reconnected;
        if (replacement is null || connection.ConnectionId != replacement)
        {
            throw new InvalidOperationException("Automatic reconnect should supply the active connection ID.");
        }

        var closed = connection.HasClosed().Take(1).ToTask(token);
        _ = await connection.StopObservable(1, token).ToTask(token);
        if (await closed is not null)
        {
            throw new InvalidOperationException("The order connection should close cleanly.");
        }

        // A UI command can start a disconnected connection when the screen returns.
        _ = await connection.StateChanges().Take(1).Start(connection, 1, token).ToTask(token);
        _ = await connection.InvokeObservable(nameof(OrderHub.Notify), token, OrderNumber).ToTask(token);
        _ = await connection.StopObservable(token).ToTask(token);
        await app.StopAsync(token);
    }

    /// <summary>Reads an order, listens for a status change and checks both against its history.</summary>
    /// <param name="connection">The connection to the order hub.</param>
    /// <param name="number">The order to read.</param>
    /// <param name="token">Cancels the hub calls.</param>
    /// <returns>Completion after the lookup, callback and stream agree.</returns>
    /// <exception cref="InvalidOperationException">The hub returned inconsistent order data.</exception>
    private static async Task CheckOrderAsync(HubConnection connection, int number, CancellationToken token)
    {
        var connected = await connection.WaitForState(HubConnectionState.Connected).ToTask(token);
        var method = new HubMethod<string>(nameof(OrderHub.Status));
        var status = await connection.InvokeObservable(method, token, number).ToTask(token);
        var callback = new HubMethod<(int Number, string Status)>("OrderChanged");
        var notification = connection.On(callback).Take(1).ToTask(token);
        _ = await connection.SendObservable(nameof(OrderHub.Notify), token, number).ToTask(token);
        var received = await notification;
        var history = await connection.StreamObservable(new HubMethod<string>(nameof(OrderHub.History)), token, number).ToList().ToTask(token);
        if (connected != HubConnectionState.Connected || method.ResultType != typeof(string) || status != $"{number} shipped"
            || received != (number, "shipped") || !history.SequenceEqual([$"{number} received", status]))
        {
            throw new InvalidOperationException("The lookup, notification and history should describe the same order.");
        }
    }
}
