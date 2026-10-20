// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace ReactiveUI.AspNetCore.SignalR.Client.Rx.Reactive.Tests;
#else
namespace ReactiveUI.AspNetCore.SignalR.Client.Rx.Tests;
#endif

/// <summary>Hosts an in-memory SignalR app for tests.</summary>
internal sealed class SignalRTestApp : IAsyncDisposable
{
    /// <summary>The in-memory test host.</summary>
    private readonly IHost _host;

    /// <summary>The HTTP transport fault injector.</summary>
    private readonly Action _failNextPoll;

    /// <summary>Initializes a new instance of the <see cref="SignalRTestApp"/> class.</summary>
    /// <param name="host">The in-memory host.</param>
    /// <param name="connection">The SignalR client connection.</param>
    /// <param name="events">The server-side test event sink.</param>
    /// <param name="failNextPoll">The HTTP transport fault injector.</param>
    private SignalRTestApp(IHost host, HubConnection connection, TestHubEvents events, Action failNextPoll)
    {
        _host = host;
        _failNextPoll = failNextPoll;
        Connection = connection;
        Events = events;
    }

    /// <summary>Gets the SignalR client connection.</summary>
    internal HubConnection Connection { get; }

    /// <summary>Gets server-side test events.</summary>
    internal TestHubEvents Events { get; }

    /// <summary>Creates and starts the in-memory SignalR host.</summary>
    /// <returns>A task that returns the test app.</returns>
    internal static async Task<SignalRTestApp> CreateAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(static webBuilder =>
            {
                _ = webBuilder
                    .UseTestServer()
                    .ConfigureServices(static services =>
                    {
                        _ = services.AddSignalR(static options => options.EnableDetailedErrors = true);
                        _ = services.AddSingleton<TestHubEvents>();
                    })
                    .Configure(static app =>
                    {
                        _ = app.UseRouting();
                        _ = app.UseEndpoints(static endpoints => _ = endpoints.MapHub<TestHub>("/testHub"));
                    });
            })
            .StartAsync()
            .ConfigureAwait(false);

        var server = host.GetTestServer();
        var failNextPoll = 0;
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/testHub", options =>
            {
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => new PollFailureHandler(server.CreateHandler(), () => Interlocked.Exchange(ref failNextPoll, 0) == 1);
            })
            .WithAutomaticReconnect([TimeSpan.Zero])
            .Build();
        var events = host.Services.GetRequiredService<TestHubEvents>();

        return new(host, connection, events, () => Interlocked.Exchange(ref failNextPoll, 1));
    }

    /// <summary>Fails the next long-poll request to interrupt the client transport.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void FailNextPoll() => _failNextPoll();

    /// <summary>Disposes the client connection and in-memory host.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Connection.DisposeAsync().ConfigureAwait(false);
        await _host.StopAsync().ConfigureAwait(false);
        _host.Dispose();
    }

    /// <summary>Injects one HTTP failure through the public long-poll transport.</summary>
    /// <param name="innerHandler">The in-memory server handler.</param>
    /// <param name="shouldFail">Consumes the pending poll failure request.</param>
    private sealed class PollFailureHandler(System.Net.Http.HttpMessageHandler innerHandler, Func<bool> shouldFail) : System.Net.Http.DelegatingHandler(innerHandler)
    {
        /// <summary>Forwards HTTP requests, failing one armed long-poll request.</summary>
        /// <param name="request">The outgoing HTTP request.</param>
        /// <param name="cancellationToken">The request cancellation token.</param>
        /// <returns>The server response or the injected transport failure.</returns>
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken) =>
            request.Method == System.Net.Http.HttpMethod.Get && shouldFail()
                ? Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.BadGateway))
                : base.SendAsync(request, cancellationToken);
    }
}
