// -----------------------------------------------------------------------
// <copyright file="StripeGlobalStateCollection.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Stripe.Tests.TestSupport;

/// <summary>
/// xUnit test collection ensuring tests that mutate the process-wide
/// <see cref="global::Stripe.StripeConfiguration.StripeClient"/> singleton run
/// serially. Without this, parallel execution would race on the shared
/// global and cause flaky failures.
/// </summary>
[CollectionDefinition(Name)]
public sealed class StripeGlobalStateCollection
{
    /// <summary>Collection name used by [Collection(...)] attributes.</summary>
    public const string Name = "stripe-global-state";
}
