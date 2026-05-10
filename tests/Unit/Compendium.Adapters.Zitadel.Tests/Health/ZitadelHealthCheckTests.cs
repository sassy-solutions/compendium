// -----------------------------------------------------------------------
// <copyright file="ZitadelHealthCheckTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Health;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Zitadel.Tests.Health;

/// <summary>
/// Unit tests for <see cref="ZitadelHealthCheck"/>.
/// </summary>
public class ZitadelHealthCheckTests
{
    private const string Authority = "https://zitadel.invalid";

    [Fact]
    public void Ctor_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new ZitadelOptions { Authority = Authority });
        var http = new HttpClient();

        // Act
        Action a1 = () => new ZitadelHealthCheck(null!, http, NullLogger<ZitadelHealthCheck>.Instance);
        Action a2 = () => new ZitadelHealthCheck(options, null!, NullLogger<ZitadelHealthCheck>.Instance);
        Action a3 = () => new ZitadelHealthCheck(options, http, null!);

        // Assert
        a1.Should().Throw<ArgumentNullException>().WithParameterName("options");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        a3.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAuthorityMissing_ReportsUnhealthy()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler(), new ZitadelOptions
        {
            Authority = string.Empty,
            ClientId = "id",
            ClientSecret = "sec"
        });

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("configuration is invalid");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientIdMissing_ReportsUnhealthy()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler(), new ZitadelOptions
        {
            Authority = Authority,
            ClientSecret = "sec"
        });

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDiscoveryReturnsError_ReportsUnhealthy()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Respond(HttpStatusCode.InternalServerError);
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("discovery endpoint returned");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenIssuerMismatches_ReportsDegraded()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Respond("application/json",
                "{\"issuer\":\"https://other.example.com\"," +
                "\"authorization_endpoint\":\"x\",\"token_endpoint\":\"y\",\"jwks_uri\":\"z\"}");
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("issuer mismatch");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenEndpointsMissing_ReportsDegraded()
    {
        // Arrange — issuer matches but jwks_uri / token_endpoint absent.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Respond("application/json", "{\"issuer\":\"" + Authority + "\"}");
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("missing required endpoints");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAllOk_ReportsHealthy()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Respond("application/json",
                "{\"issuer\":\"" + Authority + "\"," +
                "\"authorization_endpoint\":\"x\",\"token_endpoint\":\"y\",\"jwks_uri\":\"z\"}");
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OnHttpRequestException_ReportsUnhealthyAsUnreachable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Throw(new HttpRequestException("net"));
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_OnTimeout_ReportsTimedOut()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Throw(new TaskCanceledException("t", new TimeoutException("inner")));
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task CheckHealthAsync_OnUnexpectedException_ReportsUnhealthy()
    {
        // Arrange — Throw an unexpected (non-Http, non-Timeout) exception.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/.well-known/openid-configuration")
            .Throw(new InvalidOperationException("boom"));
        var sut = CreateSut(mock, ValidOptions);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
    }

    private static ZitadelOptions ValidOptions => new()
    {
        Authority = Authority,
        ClientId = "cid",
        ClientSecret = "csec",
        ProjectId = "pid"
    };

    private static ZitadelHealthCheck CreateSut(MockHttpMessageHandler mock, ZitadelOptions options)
    {
        var http = mock.ToHttpClient();
        return new ZitadelHealthCheck(Options.Create(options), http, NullLogger<ZitadelHealthCheck>.Instance);
    }
}
