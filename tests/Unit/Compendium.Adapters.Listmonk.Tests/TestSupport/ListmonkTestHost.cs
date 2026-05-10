// -----------------------------------------------------------------------
// <copyright file="ListmonkTestHost.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Listmonk.Tests.TestSupport;

/// <summary>
/// Test harness that wires Listmonk services through public DI but replaces the
/// underlying <see cref="HttpMessageHandler"/> with a <see cref="MockHttpMessageHandler"/>
/// and strips Polly retry / circuit-breaker policies so error-path tests run fast.
/// </summary>
internal sealed class ListmonkTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private bool _disposed;

    public ListmonkTestHost(MockHttpMessageHandler mockHttp, ListmonkOptions? options = null)
    {
        MockHttp = mockHttp ?? throw new ArgumentNullException(nameof(mockHttp));

        var services = new ServiceCollection();

        // Logging is required by the typed HttpClient and the services.
        services.AddLogging();

        // Default test options: a stable base url, basic auth credentials,
        // and a default list id so newsletter SubscribeAsync can fall back to it.
        var opts = options ?? new ListmonkOptions
        {
            BaseUrl = "https://listmonk.test/",
            Username = "admin",
            Password = "secret",
            DefaultFromEmail = "noreply@example.com",
            DefaultFromName = "Test",
            DefaultListId = 1,
            TimeoutSeconds = 5,
            MaxRetries = 0,
            SkipSslValidation = false
        };

        services.AddListmonk(o =>
        {
            o.BaseUrl = opts.BaseUrl;
            o.Username = opts.Username;
            o.Password = opts.Password;
            o.DefaultFromEmail = opts.DefaultFromEmail;
            o.DefaultFromName = opts.DefaultFromName;
            o.DefaultListId = opts.DefaultListId;
            o.TimeoutSeconds = opts.TimeoutSeconds;
            o.MaxRetries = opts.MaxRetries;
            o.SkipSslValidation = opts.SkipSslValidation;
        });

        // Replace the primary handler and strip Polly DelegatingHandlers so retries don't
        // multiply test runtime when we exercise transient (5xx / 429) error paths.
        // Apply to every named HTTP client created in this container — the test host only
        // uses the typed Listmonk client, so the broad post-configure is safe.
        services.PostConfigureAll<HttpClientFactoryOptions>(factoryOptions =>
        {
            factoryOptions.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.AdditionalHandlers.Clear();
                builder.PrimaryHandler = mockHttp;
            });
        });

        _provider = services.BuildServiceProvider();
    }

    public MockHttpMessageHandler MockHttp { get; }

    public IEmailService EmailService => _provider.GetRequiredService<IEmailService>();

    public INewsletterService NewsletterService => _provider.GetRequiredService<INewsletterService>();

    public ListmonkOptions Options => _provider.GetRequiredService<IOptions<ListmonkOptions>>().Value;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _provider.Dispose();
        MockHttp.Dispose();
        _disposed = true;
    }
}
