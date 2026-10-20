// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace SignalRChat.Pages;

/// <summary>Error page model.</summary>
/// <seealso cref="PageModel" />
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[DebuggerDisplay("RequestId = {RequestId}")]
public class ErrorModel : PageModel
{
    /// <summary>Gets or sets the request identifier.</summary>
    /// <value>The request identifier.</value>
    public string? RequestId { get; set; }

    /// <summary>Gets a value indicating whether the request identifier should be shown.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Populates the request identifier.</summary>
    public void OnGet() => RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}
