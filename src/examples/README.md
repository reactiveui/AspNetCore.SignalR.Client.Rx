# Order updates over SignalR

The order client starts a hub in memory and connects through SignalR's long-polling transport. It requests an order status, receives an update from the server, reads the order's history and observes the connection closing. It checks each result before exiting.

The server returns HTTP 503 for one poll. The client observes the disconnect, waits for automatic reconnection and checks the replacement connection ID. It also demonstrates how a screen event can start a stopped connection.

From `src`, run the project for your package family.

| Package | Command |
| --- | --- |
| ReactiveUI.AspNetCore.SignalR.Client.Rx | `dotnet run --project examples/OrderUpdates -f net11.0` |
| ReactiveUI.AspNetCore.SignalR.Client.Rx.Reactive | `dotnet run --project examples/OrderUpdates.Reactive -f net11.0` |

Both projects also target .NET 10. The [client](OrderUpdates/Program.cs) owns its subscriptions and the [hub](OrderUpdates/OrderHub.cs) implements the server methods. The [WPF client](../WpfClient) and [web server](../SampleWeb) show the same client library in a chat app.
