// -----------------------------------------------------------------------
// <copyright file="IWebhookSender.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Webhooks.Models;

namespace Compendium.Abstractions.Webhooks;

/// <summary>
/// Provides operations for fan-out of integration events to external consumer endpoints.
/// This port complements the in-process outbox: where the outbox publishes integration
/// events inside the application, an <see cref="IWebhookSender"/> adapter dispatches them
/// over HTTP to subscribed external endpoints with provider-managed retries and signatures.
/// Implementations may target managed services such as Svix or implement raw HTTP fan-out.
/// </summary>
public interface IWebhookSender
{
    /// <summary>
    /// Dispatches a webhook message to all endpoints subscribed to its event name within
    /// the message's tenant. Adapters MUST honour the message's <see cref="WebhookMessage.Id"/>
    /// as an idempotency key so that repeated calls produce a single logical delivery.
    /// </summary>
    /// <param name="message">The message to dispatch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A successful result on accepted-for-delivery, or an error describing the failure.</returns>
    Task<Result> SendAsync(WebhookMessage message, CancellationToken ct);

    /// <summary>
    /// Registers a new consumer endpoint for the supplied tenant.
    /// </summary>
    /// <param name="endpoint">The endpoint to register.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the provider-assigned endpoint identifier on success.</returns>
    Task<Result<string>> RegisterEndpointAsync(WebhookEndpoint endpoint, CancellationToken ct);

    /// <summary>
    /// Deletes the endpoint identified by <paramref name="endpointId"/> for the supplied tenant.
    /// </summary>
    /// <param name="endpointId">The endpoint identifier to delete.</param>
    /// <param name="tenantId">The tenant the endpoint belongs to. Adapters MUST refuse to
    /// delete endpoints owned by a different tenant.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A successful result on deletion, or <see cref="WebhookErrors.EndpointNotFound"/>
    /// if no matching endpoint exists for the tenant.</returns>
    Task<Result> DeleteEndpointAsync(string endpointId, string tenantId, CancellationToken ct);
}
