// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyTestHelpers.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Billing;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.LemonSqueezy.Tests.Helpers;

/// <summary>
/// Reflection helpers for instantiating internal types of the LemonSqueezy adapter
/// (the adapter exposes only interfaces; concrete implementations are <c>internal sealed</c>).
/// </summary>
internal static class LemonSqueezyTestHelpers
{
    private static readonly Assembly AdapterAssembly = typeof(LemonSqueezyOptions).Assembly;

    private static readonly Type HttpClientType = AdapterAssembly.GetType(
        "Compendium.Adapters.LemonSqueezy.Http.LemonSqueezyHttpClient",
        throwOnError: true)!;

    private static readonly Type BillingServiceType = AdapterAssembly.GetType(
        "Compendium.Adapters.LemonSqueezy.Services.LemonSqueezyBillingService",
        throwOnError: true)!;

    private static readonly Type SubscriptionServiceType = AdapterAssembly.GetType(
        "Compendium.Adapters.LemonSqueezy.Services.LemonSqueezySubscriptionService",
        throwOnError: true)!;

    private static readonly Type LicenseServiceType = AdapterAssembly.GetType(
        "Compendium.Adapters.LemonSqueezy.Services.LemonSqueezyLicenseService",
        throwOnError: true)!;

    private static readonly Type WebhookHandlerType = AdapterAssembly.GetType(
        "Compendium.Adapters.LemonSqueezy.Webhooks.LemonSqueezyWebhookHandler",
        throwOnError: true)!;

    /// <summary>
    /// Creates the internal <c>LemonSqueezyHttpClient</c> from a <see cref="HttpMessageHandler"/>
    /// (typically a <see cref="RichardSzalay.MockHttp.MockHttpMessageHandler"/>).
    /// </summary>
    public static object CreateHttpClient(
        HttpMessageHandler handler,
        LemonSqueezyOptions options)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/")
        };

        var optionsWrapper = Options.Create(options);
        var logger = CreateNullLogger(HttpClientType);

        return Activator.CreateInstance(
            HttpClientType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { http, optionsWrapper, logger },
            culture: null)!;
    }

    /// <summary>
    /// Creates the <see cref="IBillingService"/> implementation backed by a mock HTTP handler.
    /// </summary>
    public static IBillingService CreateBillingService(
        HttpMessageHandler handler,
        LemonSqueezyOptions options)
    {
        var httpClient = CreateHttpClient(handler, options);
        var optionsWrapper = Options.Create(options);
        var logger = CreateNullLogger(BillingServiceType);

        return (IBillingService)Activator.CreateInstance(
            BillingServiceType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { httpClient, optionsWrapper, logger },
            culture: null)!;
    }

    /// <summary>
    /// Creates the <see cref="ISubscriptionService"/> implementation backed by a mock HTTP handler.
    /// </summary>
    public static ISubscriptionService CreateSubscriptionService(
        HttpMessageHandler handler,
        LemonSqueezyOptions options)
    {
        var httpClient = CreateHttpClient(handler, options);
        var optionsWrapper = Options.Create(options);
        var logger = CreateNullLogger(SubscriptionServiceType);

        return (ISubscriptionService)Activator.CreateInstance(
            SubscriptionServiceType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { httpClient, optionsWrapper, logger },
            culture: null)!;
    }

    /// <summary>
    /// Creates the <see cref="ILicenseService"/> implementation backed by a mock HTTP handler.
    /// </summary>
    public static ILicenseService CreateLicenseService(
        HttpMessageHandler handler,
        LemonSqueezyOptions options)
    {
        var httpClient = CreateHttpClient(handler, options);
        var optionsWrapper = Options.Create(options);
        var logger = CreateNullLogger(LicenseServiceType);

        return (ILicenseService)Activator.CreateInstance(
            LicenseServiceType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { httpClient, optionsWrapper, logger },
            culture: null)!;
    }

    /// <summary>
    /// Creates the <see cref="IPaymentWebhookHandler"/> implementation. The webhook handler
    /// does not perform HTTP I/O and only requires options + logger.
    /// </summary>
    public static IPaymentWebhookHandler CreateWebhookHandler(LemonSqueezyOptions options)
    {
        var optionsWrapper = Options.Create(options);
        var logger = CreateNullLogger(WebhookHandlerType);

        return (IPaymentWebhookHandler)Activator.CreateInstance(
            WebhookHandlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { optionsWrapper, logger },
            culture: null)!;
    }

    /// <summary>
    /// Computes the lowercase hex HMAC-SHA256 LemonSqueezy webhook signature for the given payload.
    /// </summary>
    public static string ComputeWebhookSignature(string secret, string payload)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static object CreateNullLogger(Type forType)
    {
        var loggerType = typeof(NullLogger<>).MakeGenericType(forType);
        return loggerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
    }
}
