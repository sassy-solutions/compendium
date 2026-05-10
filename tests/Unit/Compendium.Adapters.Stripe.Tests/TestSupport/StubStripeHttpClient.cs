// -----------------------------------------------------------------------
// <copyright file="StubStripeHttpClient.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using Stripe;

namespace Compendium.Adapters.Stripe.Tests.TestSupport;

/// <summary>
/// Stub implementation of <see cref="IHttpClient"/> used to drive the Stripe.net
/// SDK in unit tests without performing real network I/O. Each request is matched
/// against a queue of pre-registered canned responses keyed by HTTP method and
/// URL path suffix.
/// </summary>
internal sealed class StubStripeHttpClient : IHttpClient
{
    private readonly Queue<RequestRecord> _recorded = new();
    private readonly List<Handler> _handlers = new();

    /// <summary>
    /// Gets the chronological sequence of requests handled by this stub.
    /// </summary>
    public IReadOnlyList<RequestRecord> Recorded => _recorded.ToArray();

    /// <summary>
    /// Registers a canned JSON response for the given <paramref name="method"/> and
    /// path suffix (e.g. <c>"/v1/customers"</c>). Each registered handler is one-shot
    /// FIFO: the first matching handler wins and is removed after a hit, allowing the
    /// caller to script multi-step flows.
    /// </summary>
    public void Expect(HttpMethod method, string pathSuffix, string jsonBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _handlers.Add(new Handler(method, pathSuffix, jsonBody, statusCode, OneShot: true));
    }

    /// <summary>
    /// Registers a canned JSON response that will keep matching for every request
    /// (does not consume on hit). Useful when several SDK methods share one path.
    /// </summary>
    public void ExpectPersistent(HttpMethod method, string pathSuffix, string jsonBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _handlers.Add(new Handler(method, pathSuffix, jsonBody, statusCode, OneShot: false));
    }

    /// <inheritdoc />
    public async Task<StripeResponse> MakeRequestAsync(StripeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = request.Uri.AbsolutePath;
        var bodyText = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _recorded.Enqueue(new RequestRecord(request.Method, request.Uri, bodyText));

        for (var i = 0; i < _handlers.Count; i++)
        {
            var handler = _handlers[i];
            if (handler.Method == request.Method && path.EndsWith(handler.PathSuffix, StringComparison.Ordinal))
            {
                if (handler.OneShot)
                {
                    _handlers.RemoveAt(i);
                }

                using var msg = new HttpResponseMessage(handler.StatusCode);
                return new StripeResponse(handler.StatusCode, msg.Headers, handler.JsonBody);
            }
        }

        throw new InvalidOperationException(
            $"StubStripeHttpClient: unexpected request {request.Method} {path}. Registered handlers: "
            + string.Join("; ", _handlers.ConvertAll(h => $"{h.Method} *{h.PathSuffix}")));
    }

    /// <inheritdoc />
    public Task<StripeStreamedResponse> MakeStreamingRequestAsync(StripeRequest request, CancellationToken cancellationToken = default)
    {
        // Streaming endpoints are not exercised by the Stripe adapter under test;
        // any caller hitting this path indicates a missing test-side stub setup.
        throw new NotSupportedException(
            "StubStripeHttpClient does not support streaming requests in unit tests.");
    }

    private sealed record Handler(
        HttpMethod Method,
        string PathSuffix,
        string JsonBody,
        HttpStatusCode StatusCode,
        bool OneShot);

    /// <summary>Captured request issued by the SDK against this stub.</summary>
    public sealed record RequestRecord(HttpMethod Method, Uri Uri, string? Content);
}
