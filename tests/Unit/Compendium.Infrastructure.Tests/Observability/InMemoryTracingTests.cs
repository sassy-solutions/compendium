// -----------------------------------------------------------------------
// <copyright file="InMemoryTracingTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Compendium.Infrastructure.Tests.Observability;

/// <summary>
/// Unit tests for <see cref="InMemoryTracing"/> span lifecycle and async-flow isolation.
/// </summary>
public sealed class InMemoryTracingTests
{
    private readonly ILogger<InMemoryTracing> _logger = Substitute.For<ILogger<InMemoryTracing>>();
    private readonly InMemoryTracing _sut;

    public InMemoryTracingTests()
    {
        // Arrange
        _sut = new InMemoryTracing(_logger);
    }

    [Fact]
    public void Ctor_WithNullLogger_Throws()
    {
        // Arrange / Act
        var act = () => new InMemoryTracing(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void StartSpan_WithValidName_ReturnsActiveSpanAndSetsCurrent()
    {
        // Arrange / Act
        using var span = _sut.StartSpan("op-1");

        // Assert
        span.Should().NotBeNull();
        span.OperationName.Should().Be("op-1");
        span.SpanId.Should().NotBeNullOrEmpty();
        span.TraceId.Should().NotBeNullOrEmpty();
        _sut.GetCurrentSpan().Should().BeSameAs(span);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StartSpan_WithEmptyName_Throws(string invalid)
    {
        // Arrange / Act
        var act = () => _sut.StartSpan(invalid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("operationName");
    }

    [Fact]
    public void StartSpan_WithExplicitParent_SharesTraceId()
    {
        // Arrange
        using var parent = _sut.StartSpan("parent");

        // Act
        using var child = _sut.StartSpan("child", parent);

        // Assert
        child.TraceId.Should().Be(parent.TraceId);
        child.SpanId.Should().NotBe(parent.SpanId);
    }

    [Fact]
    public void StartSpan_WithoutExplicitParent_UsesCurrentAsParent()
    {
        // Arrange
        using var outer = _sut.StartSpan("outer");

        // Act
        using var inner = _sut.StartSpan("inner");

        // Assert
        inner.TraceId.Should().Be(outer.TraceId);
    }

    [Fact]
    public void GetCurrentSpan_NoSpanStarted_ReturnsNull()
    {
        // Arrange / Act
        var current = _sut.GetCurrentSpan();

        // Assert
        current.Should().BeNull();
    }

    [Fact]
    public void SetCurrentSpan_StoresValue()
    {
        // Arrange
        using var span = _sut.StartSpan("op");

        // Act
        _sut.SetCurrentSpan(null);

        // Assert
        _sut.GetCurrentSpan().Should().BeNull();
    }

    [Fact]
    public void StartActivity_PlaceholderImplementation_ReturnsNull()
    {
        // Arrange / Act
        var activity = _sut.StartActivity("placeholder");

        // Assert
        activity.Should().BeNull();
    }

    [Fact]
    public void AddEvent_PlaceholderImplementation_DoesNotThrow()
    {
        // Arrange / Act
        var act = () => _sut.AddEvent(null, "evt", new Dictionary<string, object>());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetStatus_PlaceholderImplementation_DoesNotThrow()
    {
        // Arrange / Act
        var act = () =>
        {
            using var activity = new Activity("test");
            _sut.SetStatus(activity, true, "ok");
            _sut.SetStatus(null, false);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Span_Dispose_SetsEndTimeAndStatusOk()
    {
        // Arrange
        var span = _sut.StartSpan("disposable");

        // Act
        span.Dispose();

        // Assert
        span.EndTime.Should().NotBeNull();
        span.Duration.Should().NotBeNull();
        span.Status.Should().Be(TraceSpanStatus.Ok);
    }

    [Fact]
    public void Span_Dispose_TwiceIsIdempotent()
    {
        // Arrange
        var span = _sut.StartSpan("repeat-dispose");

        // Act
        span.Dispose();
        var firstEnd = span.EndTime;
        span.Dispose();

        // Assert
        span.EndTime.Should().Be(firstEnd);
    }

    [Fact]
    public void Span_SetStatus_StoresStatus()
    {
        // Arrange
        using var span = _sut.StartSpan("with-status");

        // Act
        span.SetStatus(TraceSpanStatus.Error, "boom");

        // Assert
        span.Status.Should().Be(TraceSpanStatus.Error);
        span.Tags.Should().ContainKey("status.description");
        span.Tags["status.description"].Should().Be("boom");
    }

    [Fact]
    public void Span_SetStatus_Disposed_StaysAtSetStatus()
    {
        // Arrange
        var span = _sut.StartSpan("error-then-dispose");
        span.SetStatus(TraceSpanStatus.Error);

        // Act
        span.Dispose();

        // Assert — when disposed and status already set (not Unset), it should stay
        span.Status.Should().Be(TraceSpanStatus.Error);
    }

    [Fact]
    public void Span_SetTag_StoresTag()
    {
        // Arrange
        using var span = _sut.StartSpan("tagged");

        // Act
        span.SetTag("user", "alice");
        span.SetTag("count", 42);

        // Assert
        span.Tags["user"].Should().Be("alice");
        span.Tags["count"].Should().Be(42);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Span_SetTag_EmptyKey_Throws(string invalid)
    {
        // Arrange
        using var span = _sut.StartSpan("invalid-tag");

        // Act
        var act = () => span.SetTag(invalid, "x");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("key");
    }

    [Fact]
    public void Span_AddEvent_StoresEvent()
    {
        // Arrange
        using var span = _sut.StartSpan("with-event");
        var ts = DateTime.UtcNow.AddMinutes(-1);

        // Act
        span.AddEvent("evt-1", ts,
            new KeyValuePair<string, object?>("k", "v"));
        span.AddEvent("evt-2");

        // Assert
        span.Events.Should().HaveCount(2);
        span.Events[0].Name.Should().Be("evt-1");
        span.Events[0].Timestamp.Should().Be(ts);
        span.Events[0].Attributes["k"].Should().Be("v");
        span.Events[1].Name.Should().Be("evt-2");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Span_AddEvent_EmptyName_Throws(string invalid)
    {
        // Arrange
        using var span = _sut.StartSpan("op");

        // Act
        var act = () => span.AddEvent(invalid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Span_RecordException_SetsErrorStatusAndAddsExceptionEvent()
    {
        // Arrange
        using var span = _sut.StartSpan("failing");
        var ex = new InvalidOperationException("kaboom");

        // Act
        span.RecordException(ex);

        // Assert
        span.Status.Should().Be(TraceSpanStatus.Error);
        span.Events.Should().ContainSingle(e => e.Name == "exception");
        var evt = span.Events.Single();
        evt.Attributes["exception.type"].Should().Be(nameof(InvalidOperationException));
        evt.Attributes["exception.message"].Should().Be("kaboom");
    }

    [Fact]
    public void Span_RecordException_NullException_Throws()
    {
        // Arrange
        using var span = _sut.StartSpan("op");

        // Act
        var act = () => span.RecordException(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Span_DurationWhileActive_IsNull()
    {
        // Arrange
        using var span = _sut.StartSpan("active");

        // Act / Assert
        span.EndTime.Should().BeNull();
        span.Duration.Should().BeNull();
    }
}
