// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyLicenseServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Abstractions.Billing;
using Compendium.Abstractions.Billing.Models;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Tests.Helpers;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.LemonSqueezy.Tests.Services;

/// <summary>
/// Unit tests for the internal <c>LemonSqueezyLicenseService</c> exercised via the public
/// <see cref="ILicenseService"/> contract.
/// </summary>
public class LemonSqueezyLicenseServiceTests
{
    private const string BaseUrl = "https://api.lemonsqueezy.com/v1/";

    private static LemonSqueezyOptions CreateOptions() => new()
    {
        ApiKey = "sk_test_license",
        StoreId = "store-l",
        BaseUrl = BaseUrl
    };

    // ============================================================================
    // ValidateLicenseAsync
    // ============================================================================

    [Fact]
    public async Task ValidateLicenseAsync_WhenValid_ReturnsValidResultWithStatusActive()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "valid": true,
          "license_key": {
            "id": 1,
            "status": "active",
            "key": "ABCD-1234-EFGH-5678",
            "activation_limit": 5,
            "activation_usage": 1,
            "created_at": "2026-01-01T00:00:00Z"
          }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("ABCD-1234-EFGH-5678", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Status.Should().Be(LicenseStatus.Active);
        result.Value.License.Should().NotBeNull();
        result.Value.License!.ActivationLimit.Should().Be(5);
        result.Value.License.ActivationCount.Should().Be(1);
    }

    [Theory]
    [InlineData("active", LicenseStatus.Active)]
    [InlineData("inactive", LicenseStatus.Inactive)]
    [InlineData("expired", LicenseStatus.Expired)]
    [InlineData("disabled", LicenseStatus.Disabled)]
    [InlineData("unknown", LicenseStatus.Active)]
    public async Task ValidateLicenseAsync_WhenValidWithStatus_MapsToLicenseStatus(
        string apiStatus, LicenseStatus expected)
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = $$"""
        {
          "valid": true,
          "license_key": { "id": 1, "status": "{{apiStatus}}", "key": "K" }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("K", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(expected);
    }

    [Fact]
    public async Task ValidateLicenseAsync_WhenInvalidAndNoLicenseData_ReturnsInactiveStatus()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", "{\"valid\":false,\"error\":\"key not found\"}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("BAD-KEY", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Status.Should().Be(LicenseStatus.Inactive);
        result.Value.ErrorMessage.Should().Be("key not found");
    }

    [Fact]
    public async Task ValidateLicenseAsync_WhenInvalidAndExpired_ReturnsExpiredStatus()
    {
        // Arrange — invalid + license data with past expiry
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "valid": false,
          "license_key": { "id": 1, "status": "active", "key": "EXP", "expires_at": "2020-01-01T00:00:00Z" }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("EXP", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Status.Should().Be(LicenseStatus.Expired);
    }

    [Fact]
    public async Task ValidateLicenseAsync_WhenInstanceProvided_ReturnsInstanceDetails()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "valid": true,
          "license_key": { "id": 1, "status": "active", "key": "K" },
          "instance": { "id": "inst-9", "name": "machine-a", "created_at": "2026-01-01T00:00:00Z" }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("K", "inst-9", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Instance.Should().NotBeNull();
        result.Value.Instance!.Id.Should().Be("inst-9");
        result.Value.Instance.Name.Should().Be("machine-a");
    }

    [Fact]
    public async Task ValidateLicenseAsync_WhenApiFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "boom");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("ANY", null, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Error");
    }

    [Fact]
    public async Task ValidateLicenseAsync_WhenLicenseKeyNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var act = async () => await sut.ValidateLicenseAsync(null!, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateLicenseAsync_WhenShortKey_LogsMaskedKeyWithoutThrowing()
    {
        // Arrange — exercises the GetKeyShort fallback for short keys
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/validate")
            .Respond("application/json", "{\"valid\":true,\"license_key\":{\"id\":1,\"status\":\"active\"}}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ValidateLicenseAsync("abc", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ============================================================================
    // ActivateLicenseAsync
    // ============================================================================

    [Fact]
    public async Task ActivateLicenseAsync_WhenSucceeds_ReturnsActivationWithInstance()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var responseJson = """
        {
          "activated": true,
          "license_key": { "id": 1, "status": "active", "key": "ABCD-1234-EFGH-5678" },
          "instance": { "id": "inst-1", "name": "host-1", "created_at": "2026-01-01T00:00:00Z" }
        }
        """;
        mock.When(HttpMethod.Post, BaseUrl + "licenses/activate")
            .Respond("application/json", responseJson);

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ActivateLicenseAsync("ABCD-1234-EFGH-5678", "host-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Activated.Should().BeTrue();
        result.Value.Instance.Should().NotBeNull();
        result.Value.Instance!.Id.Should().Be("inst-1");
        result.Value.License.Should().NotBeNull();
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenActivationLimitReached_ReturnsLimitError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/activate")
            .Respond("application/json",
                "{\"activated\":false,\"error\":\"License has reached its activation limit\"}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ActivateLicenseAsync("KEY", "host", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.LicenseActivationLimitReached");
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenInvalidKey_ReturnsInvalidLicense()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/activate")
            .Respond("application/json",
                "{\"activated\":false,\"error\":\"License key not found\"}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ActivateLicenseAsync("BAD", "host", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidLicense");
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenActivatedFalseAndNoErrorMessage_ReturnsInvalidLicense()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/activate")
            .Respond("application/json", "{\"activated\":false}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ActivateLicenseAsync("KEY", "host", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidLicense");
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenApiFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/activate")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "bad");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ActivateLicenseAsync("KEY", "host", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.BadRequest");
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenInstanceNullInResponse_ReturnsActivationWithoutInstance()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/activate")
            .Respond("application/json", "{\"activated\":true}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.ActivateLicenseAsync("KEY", "host", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Activated.Should().BeTrue();
        result.Value.Instance.Should().BeNull();
        result.Value.License.Should().BeNull();
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenLicenseKeyNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var act = async () => await sut.ActivateLicenseAsync(null!, "host", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenInstanceNameNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var act = async () => await sut.ActivateLicenseAsync("KEY", null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ============================================================================
    // DeactivateLicenseAsync
    // ============================================================================

    [Fact]
    public async Task DeactivateLicenseAsync_WhenSucceeds_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/deactivate")
            .Respond("application/json", "{\"deactivated\":true}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.DeactivateLicenseAsync("KEY", "inst-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenInstanceNotFound_ReturnsInstanceNotFoundError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/deactivate")
            .Respond("application/json",
                "{\"deactivated\":false,\"error\":\"instance not found\"}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.DeactivateLicenseAsync("KEY", "missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.LicenseInstanceNotFound");
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenDeactivationFailedGenericReason_ReturnsDeactivationFailedError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/deactivate")
            .Respond("application/json",
                "{\"deactivated\":false,\"error\":\"some other reason\"}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.DeactivateLicenseAsync("KEY", "inst-1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.DeactivationFailed");
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenDeactivatedFalseAndNoError_ReturnsDeactivationFailed()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/deactivate")
            .Respond("application/json", "{\"deactivated\":false}");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.DeactivateLicenseAsync("KEY", "inst-1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.DeactivationFailed");
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenApiFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, BaseUrl + "licenses/deactivate")
            .Respond(HttpStatusCode.Unauthorized, "text/plain", "no auth");

        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var result = await sut.DeactivateLicenseAsync("KEY", "inst-1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LemonSqueezy.Unauthorized");
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenLicenseKeyNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var act = async () => await sut.DeactivateLicenseAsync(null!, "inst", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenInstanceIdNull_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var sut = LemonSqueezyTestHelpers.CreateLicenseService(mock, CreateOptions());

        // Act
        var act = async () => await sut.DeactivateLicenseAsync("KEY", null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
