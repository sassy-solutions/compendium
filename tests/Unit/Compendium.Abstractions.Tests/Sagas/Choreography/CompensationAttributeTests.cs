// -----------------------------------------------------------------------
// <copyright file="CompensationAttributeTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Sagas.Choreography;

namespace Compendium.Abstractions.Tests.Sagas.Choreography;

public class CompensationAttributeTests
{
    private sealed record FakeForwardEvent;

    private sealed record OtherForwardEvent;

    [Compensation(typeof(FakeForwardEvent))]
    private sealed class FakeCompensationHandler
    {
        [Compensation(typeof(OtherForwardEvent))]
        public void HandleOther()
        {
        }
    }

    [Fact]
    public void CompensationAttribute_Constructor_ExposesProvidedEventType()
    {
        // Arrange / Act
        var attr = new CompensationAttribute(typeof(FakeForwardEvent));

        // Assert
        attr.CompensatesEvent.Should().Be<FakeForwardEvent>();
    }

    [Fact]
    public void CompensationAttribute_Constructor_NullEventType_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new CompensationAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("compensatesEvent");
    }

    [Fact]
    public void CompensationAttribute_AppliedOnClass_IsDiscoverableViaReflection()
    {
        // Arrange / Act
        var attr = typeof(FakeCompensationHandler).GetCustomAttribute<CompensationAttribute>(inherit: false);

        // Assert
        attr.Should().NotBeNull();
        attr!.CompensatesEvent.Should().Be<FakeForwardEvent>();
    }

    [Fact]
    public void CompensationAttribute_AppliedOnMethod_IsDiscoverableViaReflection()
    {
        // Arrange
        var method = typeof(FakeCompensationHandler).GetMethod(nameof(FakeCompensationHandler.HandleOther))!;

        // Act
        var attr = method.GetCustomAttribute<CompensationAttribute>(inherit: false);

        // Assert
        attr.Should().NotBeNull();
        attr!.CompensatesEvent.Should().Be<OtherForwardEvent>();
    }

    [Fact]
    public void CompensationAttribute_UsageMetadata_TargetsClassAndMethodWithoutInheritanceOrMultiple()
    {
        // Arrange
        var usage = typeof(CompensationAttribute).GetCustomAttribute<AttributeUsageAttribute>(inherit: false);

        // Act / Assert
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeFalse();
    }

    [Fact]
    public void CompensationAttribute_TypeIsSealed()
    {
        // Arrange / Act / Assert
        typeof(CompensationAttribute).IsSealed.Should().BeTrue();
    }
}
