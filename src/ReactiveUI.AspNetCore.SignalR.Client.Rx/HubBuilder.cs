// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace ReactiveUI.AspNetCore.SignalR.Client.Rx.Reactive;
#else
namespace ReactiveUI.AspNetCore.SignalR.Client.Rx;
#endif

/// <summary>Builds observable SignalR hub connections.</summary>
public static class HubBuilder
{
    /// <summary>Creates a HubConnection.</summary>
    /// <param name="hubConnectionBuilder">The hub connection builder.</param>
    /// <returns>A HubConnection.</returns>
    /// <exception cref="ArgumentNullException">The hub connection builder is null.</exception>
    public static IObservable<(HubConnection HubConnection, CompositeDisposable Disposables)> Create(Func<HubConnectionBuilder, IHubConnectionBuilder> hubConnectionBuilder)
    {
        _ = hubConnectionBuilder ?? throw new ArgumentNullException(nameof(hubConnectionBuilder));

        return Observable.CreateSafe<(HubConnection HubConnection, CompositeDisposable Disposables)>(observer =>
        {
            var disposables = new CompositeDisposable();
            var connection = hubConnectionBuilder(new()).Build();
            observer.OnNext((connection, disposables));
            disposables.Add(Disposable.Create(connection, DisposeConnection));
            return disposables;
        });
    }

    /// <summary>Disposes the hub connection.</summary>
    /// <param name="connection">The hub connection.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DisposeConnection(HubConnection connection) =>
        Observable.FromAsync(() => DisposeConnectionAsync(connection)).Subscribe();

    /// <summary>Asynchronously disposes a hub connection.</summary>
    /// <param name="connection">The hub connection.</param>
    /// <returns>A task that completes after disposal.</returns>
    private static async Task<RxVoid> DisposeConnectionAsync(HubConnection connection)
    {
        await connection.DisposeAsync().ConfigureAwait(false);
        return RxVoid.Default;
    }
}
