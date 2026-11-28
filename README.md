<div align="center">

<a href="https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx"><img src="https://raw.githubusercontent.com/reactiveui/styleguide/master/logo/logo.svg" alt="ReactiveUI.AspNetCore.SignalR.Client.Rx" width="128"></a>

# ReactiveUI.AspNetCore.SignalR.Client.Rx

[![Build](https://img.shields.io/github/actions/workflow/status/reactiveui/AspNetCore.SignalR.Client.Rx/BuildOnly.yml?branch=main&label=build&logo=githubactions&logoColor=white)](https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx/actions/workflows/BuildOnly.yml)
[![License](https://img.shields.io/github/license/reactiveui/AspNetCore.SignalR.Client.Rx?color=blue)](https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx/blob/main/LICENSE)
[![Discussions](https://img.shields.io/badge/discussions-ask%20a%20question-8A2BE2?logo=github&logoColor=white)](https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx/discussions)

</div>

ReactiveUI.Primitives extensions for Microsoft.AspNetCore.SignalR.Client.

## Installation

```powershell
Install-Package ReactiveUI.AspNetCore.SignalR.Client.Rx
```

Use the lean package above when the application does not otherwise depend on System.Reactive. Applications that require System.Reactive interoperability can install the shared-source variant:

```powershell
Install-Package ReactiveUI.AspNetCore.SignalR.Client.Rx.Reactive
```

## Overview

ReactiveUI.AspNetCore.SignalR.Client.Rx provides high-performance, memory-efficient observable APIs for SignalR client applications. The lean package is based on `ReactiveUI.Primitives`; the `.Reactive` package uses `ReactiveUI.Primitives.Reactive` and the `ReactiveUI.AspNetCore.SignalR.Client.Rx.Reactive` namespace.

- **Targets:** .NET Framework 4.6.2, .NET 8, .NET 9, .NET 10, and .NET 11
- **Supports:** WPF, WinForms, Console, ASP.NET Core, Razor Pages, and more

## Quick Start

```csharp
using ReactiveUI.AspNetCore.SignalR.Client.Rx;
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hub")
    .WithAutomaticReconnect()
    .Build();

// Subscribe to a hub method
var sub = connection.On(new HubMethod<string>("Stream")).Subscribe(Console.WriteLine);

// Start the connection reactively
connection.StartObservable().Subscribe();
```

## Advanced Usage

### Using HubBuilder for Lifecycle Management

```csharp
using ReactiveUI.AspNetCore.SignalR.Client.Rx;

HubBuilder.Create(builder => builder.WithUrl("https://localhost:53933/ChatHub"))
    .Subscribe(x =>
    {
        var connection = x.HubConnection;
        var disposables = x.Disposables;

        // Listen for messages
        disposables.Add(connection.On(new HubMethod<(string, string)>("ReceiveMessage"))
            .Subscribe(msg => Console.WriteLine($"{msg.Value1}: {msg.Value2}")));

        // Handle connection closed
        disposables.Add(connection.HasClosed().Subscribe(error =>
            Console.WriteLine($"Closed: {error?.Message}")));

        // Start the connection from any observable trigger.
        // disposables.Add(connectRequested.Start(connection).Subscribe());
    });
```

### Streaming from the Server

```csharp
connection.StreamObservable(new HubMethod<int>("Counter"), CancellationToken.None, 10)
    .Subscribe(
        value => Console.WriteLine($"Received: {value}"),
        ex => Console.WriteLine($"Error: {ex.Message}"),
        () => Console.WriteLine("Stream completed"));
```

### Invoking Methods and Sending Data

```csharp
// Invoke a method and get a result
connection.InvokeObservable(new HubMethod<int>("AddNumbers"), CancellationToken.None, 1, 2)
    .Subscribe(result => Console.WriteLine($"Sum: {result}"));

// Send a method (fire-and-forget)
connection.SendObservable("Notify", CancellationToken.None, "Hello!")
    .Subscribe(_ => Console.WriteLine("Notification sent"));
```

### Observing Connection State

```csharp
connection.StateChanges().Subscribe(state =>
    Console.WriteLine($"Connection state: {state}"));

// Wait for a specific state
connection.WaitForState(HubConnectionState.Connected)
    .Subscribe(_ => Console.WriteLine("Connected!"));
```

### Razor Pages Example

```csharp
@page
@using ReactiveUI.AspNetCore.SignalR.Client.Rx
@inject Microsoft.AspNetCore.SignalR.Client.HubConnection Connection

@functions {
    protected override void OnInitialized()
    {
        Connection.On(new HubMethod<string>("ReceiveMessage"))
            .Subscribe(msg => /* update UI */);
        Connection.StartObservable().Subscribe();
    }
}
```

## API Reference

### Event Subscription
- `On(string methodName)` — No-arg handler
- `On<T>(HubMethod<T> method)`
- `On<T1, T2>(HubMethod<(T1, T2)> method)`
- ... up to 8 arguments

### Connection Lifecycle
- `StartObservable()` / `StartObservable(CancellationToken)`
- `StartObservable(int? retryCount, CancellationToken)`
- `StopObservable()` / `StopObservable(CancellationToken)`
- `StopObservable(int? retryCount, CancellationToken)`
- `EnsureStarted()` / `EnsureStarted(CancellationToken)`
- `StateChanges()`
- `WaitForState(HubConnectionState)`

### Streaming
- `StreamObservable<T>(HubMethod<T> method, CancellationToken, params object?[] args)`

### Invocation
- `InvokeObservable<T>(HubMethod<T> method, CancellationToken, params object?[] args)`
- `InvokeObservable(string methodName, CancellationToken, params object?[] args)`
- `SendObservable(string methodName, CancellationToken, params object?[] args)`

### Connection Events
- `HasClosed()`
- `IsReconnecting()`
- `HasReconnected()`

### HubBuilder
- `HubBuilder.Create(Func<HubConnectionBuilder, IHubConnectionBuilder>)`

## Best Practices
- Always dispose subscriptions to avoid memory leaks.
- Use the disposable scope returned by `HubBuilder.Create` for managing multiple subscriptions.
- Prefer `StartObservable`/`StopObservable` for connection management in Rx scenarios.
- Use `StreamObservable` for server-to-client streaming.

## License
MIT

## See Also
- [Microsoft.AspNetCore.SignalR.Client Documentation](https://learn.microsoft.com/aspnet/core/signalr/dotnet-client)
- [Reactive Extensions (Rx)](https://github.com/dotnet/reactive)

---

**ReactiveUI.AspNetCore.SignalR.Client.Rx** - Empowering Industrial Automation with Reactive Technology ⚡🏭

## Contribute

ReactiveUI.AspNetCore.SignalR.Client.Rx is part of [ReactiveUI](https://github.com/reactiveui) and is released under the [MIT license](https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx/blob/main/LICENSE). We welcome contributions of every size, from bug reports and documentation to features and tests.

- **Questions:** ask in [GitHub Discussions](https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx/discussions).
- **Bugs and feature requests:** open a [GitHub issue](https://github.com/reactiveui/AspNetCore.SignalR.Client.Rx/issues/new/choose).
- **Larger changes:** open an issue or discussion first, so we can agree on the approach before you start.
- **Contributor guide:** read [reactiveui.net/contribute](https://www.reactiveui.net/contribute/) for [building and testing](https://www.reactiveui.net/contribute/building-and-testing/) and [pull requests](https://www.reactiveui.net/contribute/pull-requests/).

### Coding standards

Every pull request must meet these standards. The build checks most of them.

- Follow the [ReactiveUI C# style guide](https://www.reactiveui.net/contribute/software-style-guide/code-style/).
- The repository's `.editorconfig` and the StyleSharp, PerformanceSharp and SecuritySharp analyzers run on every build. Fix each finding in the code. Do not suppress it or relax the `.editorconfig`.
- Add tests for every bug fix and new feature.
- Document public APIs with XML documentation comments.
- Write commit messages in the [Conventional Commits](https://www.reactiveui.net/contribute/software-style-guide/commit-message-convention/) format.
- Follow the [Code of Conduct](https://github.com/reactiveui/.github/blob/main/CODE_OF_CONDUCT.md).

### Support the project

- Sponsor the maintainers on GitHub Sponsors: [reactivemarbles](https://github.com/sponsors/reactivemarbles), [glennawatson](https://github.com/sponsors/glennawatson) and [chrispulman](https://github.com/sponsors/chrispulman).
- Ask your employer to support open source, for example with the [balanced employee IP agreement](https://github.com/github/balanced-employee-ip-agreement).

## Sponsors

[JetBrains](https://www.jetbrains.com/) gives ReactiveUI's maintainers licences for its tools through its
[open source support programme](https://www.jetbrains.com/community/opensource/).
[Anthropic](https://www.anthropic.com/) supports them with [Claude](https://claude.com/) through
[Claude for Open Source](https://claude.com/contact-sales/claude-for-oss).
[OpenAI](https://openai.com/) supports them with [Codex](https://openai.com/codex/) through
[Codex for Open Source](https://developers.openai.com/community/codex-for-oss).
[Snyk](https://snyk.io/) gives the maintainers access to security tools through its
[Secure Developer Program](https://snyk.io/open-source/).

[![JetBrains](https://raw.githubusercontent.com/reactiveui/website/main/docs/images/sponsors/jetbrains.svg)](https://www.jetbrains.com/)
[![Claude by Anthropic](https://raw.githubusercontent.com/reactiveui/website/main/docs/images/sponsors/claude.svg)](https://claude.com/)
[![OpenAI](https://raw.githubusercontent.com/reactiveui/website/main/docs/images/sponsors/openai.svg)](https://openai.com/codex/)
[![Sponsored by Snyk](https://raw.githubusercontent.com/reactiveui/website/e37a112f97ea2e9ea54b21bb7ed9e56df034d618/docs/images/sponsors/snyk.svg)](https://snyk.io/)

See [our sponsors](https://www.reactiveui.net/sponsors/) for more information.
JetBrains, Claude, Anthropic, OpenAI and Codex names and logos are trademarks of their respective owners.
Snyk and the Snyk logo are trademarks of Snyk Ltd.
