// -----------------------------------------------------------------------
// <copyright file="EventUpcasterBaseTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing;

namespace Compendium.Core.Tests.EventSourcing;

/// <summary>
/// Tests for the non-generic <see cref="IEventUpcaster"/> contract surface implemented by
/// <see cref="EventUpcasterBase{TSource, TTarget}"/> (CanUpcast, runtime Upcast dispatch,
/// SourceEventType / TargetEventType reflection).
/// </summary>
public class EventUpcasterBaseTests
{
    private sealed class FooV1 : DomainEventBase
    {
        public FooV1(string id) : base(id, "FooAggregate", 0, eventVersion: 1)
        {
        }
    }

    private sealed class FooV2 : DomainEventBase
    {
        public FooV2(string id) : base(id, "FooAggregate", 0, eventVersion: 2)
        {
        }
    }

    private sealed class FooV1ToV2Upcaster : EventUpcasterBase<FooV1, FooV2>
    {
        public override int SourceVersion => 1;

        public override int TargetVersion => 2;

        public override FooV2 Upcast(FooV1 sourceEvent)
        {
            return new FooV2(sourceEvent.AggregateId);
        }
    }

    [Fact]
    public void SourceEventType_ReturnsTSourceType()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();

        // Act / Assert
        upcaster.SourceEventType.Should().Be(typeof(FooV1));
    }

    [Fact]
    public void TargetEventType_ReturnsTTargetType()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();

        // Act / Assert
        upcaster.TargetEventType.Should().Be(typeof(FooV2));
    }

    [Fact]
    public void CanUpcast_WithMatchingTypeAndVersion_ReturnsTrue()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();

        // Act
        var result = upcaster.CanUpcast(typeof(FooV1), 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanUpcast_WithMismatchedType_ReturnsFalse()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();

        // Act
        var result = upcaster.CanUpcast(typeof(FooV2), 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanUpcast_WithMismatchedVersion_ReturnsFalse()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();

        // Act
        var result = upcaster.CanUpcast(typeof(FooV1), 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanUpcast_WithNullEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();

        // Act
        var act = () => upcaster.CanUpcast(null!, 1);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Upcast_WithNullSourceEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();
        IEventUpcaster nonGeneric = upcaster;

        // Act
        var act = () => nonGeneric.Upcast(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Upcast_NonGeneric_WithMatchingType_ReturnsUpcastedEvent()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();
        IEventUpcaster nonGeneric = upcaster;
        var src = new FooV1("agg-1");

        // Act
        var result = nonGeneric.Upcast(src);

        // Assert
        result.Should().BeOfType<FooV2>();
        result.AggregateId.Should().Be("agg-1");
        result.EventVersion.Should().Be(2);
    }

    [Fact]
    public void Upcast_NonGeneric_WithWrongType_ThrowsInvalidOperationException()
    {
        // Arrange
        var upcaster = new FooV1ToV2Upcaster();
        IEventUpcaster nonGeneric = upcaster;
        IDomainEvent wrongTypeEvent = new FooV2("agg-1");

        // Act
        var act = () => nonGeneric.Upcast(wrongTypeEvent);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot upcast event of type*");
    }

    /// <summary>
    /// FooV1 variant whose constructor declares an EventVersion of 7 — the upcaster expects 1,
    /// so the version-mismatch branch of <see cref="EventUpcasterBase{TSource, TTarget}.Upcast(IDomainEvent)"/>
    /// is exercised even though the concrete type matches.
    /// </summary>
    private sealed class FooV1WithMismatchedVersion : DomainEventBase
    {
        public FooV1WithMismatchedVersion(string id) : base(id, "FooAggregate", 0, eventVersion: 7)
        {
        }
    }

    private sealed class FooV1WithMismatchedVersionUpcaster
        : EventUpcasterBase<FooV1WithMismatchedVersion, FooV2>
    {
        // Source version intentionally mismatches the declared EventVersion of 7 above.
        public override int SourceVersion => 1;

        public override int TargetVersion => 2;

        public override FooV2 Upcast(FooV1WithMismatchedVersion sourceEvent)
        {
            return new FooV2(sourceEvent.AggregateId);
        }
    }

    [Fact]
    public void Upcast_NonGeneric_WithWrongVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var upcaster = new FooV1WithMismatchedVersionUpcaster();
        IEventUpcaster nonGeneric = upcaster;
        var src = new FooV1WithMismatchedVersion("agg-1");

        // Act
        var act = () => nonGeneric.Upcast(src);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot upcast event version*");
    }
}
