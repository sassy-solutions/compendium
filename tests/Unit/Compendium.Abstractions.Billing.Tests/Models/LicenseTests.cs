// -----------------------------------------------------------------------
// <copyright file="LicenseTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Tests.Models;

public class LicenseValidationResultTests
{
    [Fact]
    public void LicenseValidationResult_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var result = new LicenseValidationResult
        {
            IsValid = true,
            LicenseKey = "LIC-1",
            Status = LicenseStatus.Active
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.LicenseKey.Should().Be("LIC-1");
        result.Status.Should().Be(LicenseStatus.Active);
        result.ErrorMessage.Should().BeNull();
        result.License.Should().BeNull();
        result.Instance.Should().BeNull();
        result.Meta.Should().BeNull();
    }

    [Fact]
    public void LicenseValidationResult_FailedValidation_CapturesErrorMessage()
    {
        // Arrange & Act
        var result = new LicenseValidationResult
        {
            IsValid = false,
            LicenseKey = "LIC-BAD",
            Status = LicenseStatus.Disabled,
            ErrorMessage = "License is disabled"
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(LicenseStatus.Disabled);
        result.ErrorMessage.Should().Be("License is disabled");
    }

    [Fact]
    public void LicenseValidationResult_WithLicenseAndInstance_PreservesNestedDetails()
    {
        // Arrange
        var details = new LicenseDetails
        {
            Id = "lic-det-1",
            Key = "LIC-1",
            Status = LicenseStatus.Active
        };
        var instance = new LicenseInstance
        {
            Id = "inst-1",
            Name = "Workstation",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var meta = new Dictionary<string, object> { ["source"] = "api" };

        // Act
        var result = new LicenseValidationResult
        {
            IsValid = true,
            LicenseKey = "LIC-1",
            Status = LicenseStatus.Active,
            License = details,
            Instance = instance,
            Meta = meta
        };

        // Assert
        result.License.Should().BeSameAs(details);
        result.Instance.Should().BeSameAs(instance);
        result.Meta.Should().BeSameAs(meta);
    }

    [Fact]
    public void LicenseValidationResult_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = new LicenseValidationResult
        {
            IsValid = true,
            LicenseKey = "LIC-1",
            Status = LicenseStatus.Active
        };
        var second = new LicenseValidationResult
        {
            IsValid = true,
            LicenseKey = "LIC-1",
            Status = LicenseStatus.Active
        };

        // Act & Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}

public class LicenseDetailsTests
{
    private static LicenseDetails Build(
        string id = "lic-1",
        string key = "LIC-1",
        LicenseStatus status = LicenseStatus.Active,
        DateTimeOffset? expiresAt = null) =>
        new()
        {
            Id = id,
            Key = key,
            Status = status,
            ExpiresAt = expiresAt
        };

    [Fact]
    public void LicenseDetails_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var details = Build();

        // Assert
        details.Id.Should().Be("lic-1");
        details.Key.Should().Be("LIC-1");
        details.Status.Should().Be(LicenseStatus.Active);
        details.StoreId.Should().BeNull();
        details.OrderId.Should().BeNull();
        details.OrderItemId.Should().BeNull();
        details.ProductId.Should().BeNull();
        details.CustomerName.Should().BeNull();
        details.CustomerEmail.Should().BeNull();
        details.ActivationLimit.Should().BeNull();
        details.ActivationCount.Should().BeNull();
        details.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void LicenseDetails_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-30);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var details = new LicenseDetails
        {
            Id = "lic-2",
            Key = "LIC-2",
            StoreId = "store-1",
            OrderId = "ord-1",
            OrderItemId = "item-1",
            ProductId = "prod-1",
            CustomerName = "Alice",
            CustomerEmail = "alice@example.com",
            Status = LicenseStatus.Active,
            ActivationLimit = 5,
            ActivationCount = 2,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };

        // Assert
        details.StoreId.Should().Be("store-1");
        details.OrderId.Should().Be("ord-1");
        details.OrderItemId.Should().Be("item-1");
        details.ProductId.Should().Be("prod-1");
        details.CustomerName.Should().Be("Alice");
        details.CustomerEmail.Should().Be("alice@example.com");
        details.ActivationLimit.Should().Be(5);
        details.ActivationCount.Should().Be(2);
        details.CreatedAt.Should().Be(createdAt);
        details.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsNull_ReturnsFalse()
    {
        // Arrange
        var details = Build(expiresAt: null);

        // Act & Assert
        details.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInTheFuture_ReturnsFalse()
    {
        // Arrange
        var details = Build(expiresAt: DateTimeOffset.UtcNow.AddDays(7));

        // Act & Assert
        details.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInThePast_ReturnsTrue()
    {
        // Arrange
        var details = Build(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        // Act & Assert
        details.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void LicenseDetails_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = Build();
        var second = Build();

        // Act & Assert
        first.Should().Be(second);
    }

    [Fact]
    public void LicenseDetails_WithExpression_ProducesNewInstanceWithUpdatedValue()
    {
        // Arrange
        var original = Build(status: LicenseStatus.Active);

        // Act
        var disabled = original with { Status = LicenseStatus.Disabled };

        // Assert
        disabled.Status.Should().Be(LicenseStatus.Disabled);
        original.Status.Should().Be(LicenseStatus.Active);
    }
}

public class LicenseInstanceTests
{
    [Fact]
    public void LicenseInstance_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var instance = new LicenseInstance
        {
            Id = "inst-1",
            Name = "Workstation",
            CreatedAt = createdAt
        };

        // Assert
        instance.Id.Should().Be("inst-1");
        instance.Name.Should().Be("Workstation");
        instance.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void LicenseInstance_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var first = new LicenseInstance { Id = "inst-1", Name = "Wks", CreatedAt = createdAt };
        var second = new LicenseInstance { Id = "inst-1", Name = "Wks", CreatedAt = createdAt };

        // Act & Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void LicenseInstance_TwoInstancesDifferingById_AreNotEqual()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var first = new LicenseInstance { Id = "inst-1", Name = "Wks", CreatedAt = createdAt };
        var second = new LicenseInstance { Id = "inst-2", Name = "Wks", CreatedAt = createdAt };

        // Act & Assert
        first.Should().NotBe(second);
    }
}

public class LicenseActivationTests
{
    [Fact]
    public void LicenseActivation_WithRequiredProperty_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var activation = new LicenseActivation { Activated = true };

        // Assert
        activation.Activated.Should().BeTrue();
        activation.Instance.Should().BeNull();
        activation.License.Should().BeNull();
        activation.ErrorMessage.Should().BeNull();
        activation.Meta.Should().BeNull();
    }

    [Fact]
    public void LicenseActivation_FailedActivation_CapturesErrorMessage()
    {
        // Arrange & Act
        var activation = new LicenseActivation
        {
            Activated = false,
            ErrorMessage = "Activation limit reached"
        };

        // Assert
        activation.Activated.Should().BeFalse();
        activation.ErrorMessage.Should().Be("Activation limit reached");
    }

    [Fact]
    public void LicenseActivation_WithInstanceLicenseAndMeta_PreservesNestedDetails()
    {
        // Arrange
        var instance = new LicenseInstance
        {
            Id = "inst-1",
            Name = "Wks",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var details = new LicenseDetails
        {
            Id = "lic-1",
            Key = "LIC-1",
            Status = LicenseStatus.Active
        };
        var meta = new Dictionary<string, object> { ["activated_via"] = "api" };

        // Act
        var activation = new LicenseActivation
        {
            Activated = true,
            Instance = instance,
            License = details,
            Meta = meta
        };

        // Assert
        activation.Instance.Should().BeSameAs(instance);
        activation.License.Should().BeSameAs(details);
        activation.Meta.Should().BeSameAs(meta);
    }

    [Fact]
    public void LicenseActivation_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = new LicenseActivation { Activated = true };
        var second = new LicenseActivation { Activated = true };

        // Act & Assert
        first.Should().Be(second);
    }
}

public class LicenseStatusTests
{
    [Fact]
    public void LicenseStatus_DeclaresAllExpectedValues()
    {
        // Act
        var values = Enum.GetValues<LicenseStatus>();

        // Assert
        values.Should().Contain(new[]
        {
            LicenseStatus.Inactive,
            LicenseStatus.Active,
            LicenseStatus.Expired,
            LicenseStatus.Disabled
        });
    }

    [Theory]
    [InlineData(LicenseStatus.Inactive, 0)]
    [InlineData(LicenseStatus.Active, 1)]
    [InlineData(LicenseStatus.Expired, 2)]
    [InlineData(LicenseStatus.Disabled, 3)]
    public void LicenseStatus_NumericValues_AreStable(LicenseStatus status, int expected)
    {
        // Act & Assert
        ((int)status).Should().Be(expected);
    }
}
