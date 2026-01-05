// -----------------------------------------------------------------------
// <copyright file="IPaymentWebhookHandler.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing.Models;

namespace Compendium.Abstractions.Billing;

/// <summary>
/// Provides operations for processing payment webhooks from billing providers.
/// </summary>
public interface IPaymentWebhookHandler
{
    /// <summary>
    /// Processes a webhook payload from the billing provider.
    /// </summary>
    /// <param name="payload">The raw webhook payload (typically JSON).</param>
    /// <param name="signature">The webhook signature for validation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the processing result or an error.</returns>
    Task<Result<WebhookProcessingResult>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
