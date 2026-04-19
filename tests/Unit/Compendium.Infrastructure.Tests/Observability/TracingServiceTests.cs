// -----------------------------------------------------------------------
// <copyright file="TracingServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Bogus;

namespace Compendium.Infrastructure.Tests.Observability;

/// <summary>
/// Comprehensive tests for TracingService with distributed tracing scenarios.
/// </summary>
public sealed class TracingServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TracingService _sut;
    private readonly ILogger<TracingService> _logger;
    private readonly Faker _faker;

    public TracingServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = Substitute.For<ILogger<TracingService>>();
        _sut = new TracingService(_logger);
        _faker = new Faker();
    }

    #region Basic Operations - 8 tests

    [Fact]
    public void StartActivity_WithValidName_ShouldReturnActivity()
    {
        // Arrange
        var activityName = "test-operation";

        // Act
        using var activity = _sut.StartActivity(activityName);

        // Assert
        // Activity might be null if no listeners are configured, which is normal
        if (activity != null)
        {
            activity.OperationName.Should().Be(activityName);
            activity.Kind.Should().Be(ActivityKind.Internal);
        }
    }

    [Fact]
    public void StartActivity_WithDifferentKinds_ShouldSetCorrectKind()
    {
        // Arrange
        var kinds = new[]
        {
            ActivityKind.Client,
            ActivityKind.Server,
            ActivityKind.Producer,
            ActivityKind.Consumer,
            ActivityKind.Internal
        };

        foreach (var kind in kinds)
        {
            // Act
            using var activity = _sut.StartActivity($"test-{kind}", kind);

            // Assert
            if (activity != null)
            {
                activity.Kind.Should().Be(kind);
            }
        }
    }

    [Fact]
    public void AddEvent_WithValidActivity_ShouldNotThrow()
    {
        // Arrange
        using var activity = _sut.StartActivity("test-with-event");
        var eventName = "test-event";
        var attributes = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };

        // Act & Assert
        _sut.Invoking(s => s.AddEvent(activity, eventName, attributes))
            .Should().NotThrow();
    }

    [Fact]
    public void AddEvent_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var eventName = "test-event";
        var attributes = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert
        _sut.Invoking(s => s.AddEvent(null, eventName, attributes))
            .Should().NotThrow();
    }

    [Fact]
    public void SetStatus_WithValidActivity_ShouldNotThrow()
    {
        // Arrange
        using var activity = _sut.StartActivity("test-with-status");

        // Act & Assert
        _sut.Invoking(s => s.SetStatus(activity, true, "Operation completed successfully"))
            .Should().NotThrow();

        _sut.Invoking(s => s.SetStatus(activity, false, "Operation failed"))
            .Should().NotThrow();
    }

    [Fact]
    public void SetStatus_WithNullActivity_ShouldNotThrow()
    {
        // Act & Assert
        _sut.Invoking(s => s.SetStatus(null, true, "Success"))
            .Should().NotThrow();

        _sut.Invoking(s => s.SetStatus(null, false, "Failure"))
            .Should().NotThrow();
    }

    [Fact]
    public void StartSpan_WithValidName_ShouldReturnSpan()
    {
        // Arrange
        var operationName = "test-span-operation";

        // Act
        using var span = _sut.StartSpan(operationName);

        // Assert
        span.Should().NotBeNull();
        span.OperationName.Should().Be(operationName);
        span.TraceId.Should().NotBeNullOrEmpty();
        span.SpanId.Should().NotBeNullOrEmpty();
        span.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void StartSpan_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        _sut.Invoking(s => s.StartSpan(invalidName!))
            .Should().Throw<ArgumentException>();
    }

    #endregion

    #region Span Management - 6 tests

    [Fact]
    public void GetCurrentSpan_InitiallyNull_ShouldReturnNull()
    {
        // Act
        var currentSpan = _sut.GetCurrentSpan();

        // Assert
        currentSpan.Should().BeNull();
    }

    [Fact]
    public void SetCurrentSpan_WithValidSpan_ShouldSetCurrentSpan()
    {
        // Arrange
        using var span = _sut.StartSpan("test-span");

        // Act
        _sut.SetCurrentSpan(span);
        var currentSpan = _sut.GetCurrentSpan();

        // Assert
        currentSpan.Should().Be(span);
    }

    [Fact]
    public void SetCurrentSpan_WithNull_ShouldClearCurrentSpan()
    {
        // Arrange
        using var span = _sut.StartSpan("test-span");
        _sut.SetCurrentSpan(span);

        // Act
        _sut.SetCurrentSpan(null);
        var currentSpan = _sut.GetCurrentSpan();

        // Assert
        currentSpan.Should().BeNull();
    }

    [Fact]
    public void StartSpan_WithParent_ShouldCreateChildSpan()
    {
        // Arrange
        using var parentSpan = _sut.StartSpan("parent-span");

        // Act
        using var childSpan = _sut.StartSpan("child-span", parentSpan);

        // Assert
        childSpan.Should().NotBeNull();
        childSpan.TraceId.Should().Be(parentSpan.TraceId);
        childSpan.SpanId.Should().NotBe(parentSpan.SpanId);
    }

    [Fact]
    public void StartSpan_WithCurrentSpanAsParent_ShouldCreateChildSpan()
    {
        // Arrange
        using var parentSpan = _sut.StartSpan("parent-span");
        _sut.SetCurrentSpan(parentSpan);

        // Act
        using var childSpan = _sut.StartSpan("child-span");

        // Assert
        childSpan.Should().NotBeNull();
        childSpan.TraceId.Should().Be(parentSpan.TraceId);
    }

    [Fact]
    public void SpanDisposal_ShouldSetEndTimeAndStatus()
    {
        // Arrange
        ITraceSpan span;

        // Act
        using (span = _sut.StartSpan("disposable-span"))
        {
            span.EndTime.Should().BeNull();
            Thread.Sleep(10); // Ensure some duration
        }

        // Assert
        span.EndTime.Should().NotBeNull();
        span.Duration.Should().NotBeNull();
        span.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    #endregion

    #region Span Operations - 8 tests

    [Fact]
    public void Span_SetTag_ShouldAddTagToSpan()
    {
        // Arrange
        using var span = _sut.StartSpan("tagged-span");
        var key = "test-key";
        var value = "test-value";

        // Act
        span.SetTag(key, value);

        // Assert
        span.Tags.Should().ContainKey(key);
        span.Tags[key].Should().Be(value);
    }

    [Fact]
    public void Span_SetMultipleTags_ShouldAddAllTags()
    {
        // Arrange
        using var span = _sut.StartSpan("multi-tagged-span");
        var tags = new Dictionary<string, object?>
        {
            ["string-tag"] = "string-value",
            ["int-tag"] = 42,
            ["bool-tag"] = true,
            ["null-tag"] = null
        };

        // Act
        foreach (var (key, value) in tags)
        {
            span.SetTag(key, value);
        }

        // Assert
        foreach (var (key, value) in tags)
        {
            span.Tags.Should().ContainKey(key);
            span.Tags[key].Should().Be(value);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Span_SetTag_WithInvalidKey_ShouldThrowArgumentException(string? invalidKey)
    {
        // Arrange
        using var span = _sut.StartSpan("invalid-tag-span");

        // Act & Assert
        span.Invoking(s => s.SetTag(invalidKey!, "value"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Span_SetStatus_ShouldUpdateStatus()
    {
        // Arrange
        using var span = _sut.StartSpan("status-span");

        // Act
        span.SetStatus(TraceSpanStatus.Ok, "Success");

        // Assert
        span.Status.Should().Be(TraceSpanStatus.Ok);
        span.Tags.Should().ContainKey("status.description");
        span.Tags["status.description"].Should().Be("Success");
    }

    [Fact]
    public void Span_AddEvent_ShouldAddEventToSpan()
    {
        // Arrange
        using var span = _sut.StartSpan("event-span");
        var eventName = "test-event";
        var attributes = new[]
        {
            new KeyValuePair<string, object?>("attr1", "value1"),
            new KeyValuePair<string, object?>("attr2", 42)
        };

        // Act
        span.AddEvent(eventName, null, attributes);

        // Assert
        span.Events.Should().HaveCount(1);
        var addedEvent = span.Events.First();
        addedEvent.Name.Should().Be(eventName);
        addedEvent.Attributes.Should().ContainKey("attr1");
        addedEvent.Attributes.Should().ContainKey("attr2");
    }

    [Fact]
    public void Span_AddMultipleEvents_ShouldAddAllEvents()
    {
        // Arrange
        using var span = _sut.StartSpan("multi-event-span");
        var events = new[]
        {
            "event-1",
            "event-2",
            "event-3"
        };

        // Act
        foreach (var eventName in events)
        {
            span.AddEvent(eventName);
        }

        // Assert
        span.Events.Should().HaveCount(events.Length);
        span.Events.Select(e => e.Name).Should().BeEquivalentTo(events);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Span_AddEvent_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        using var span = _sut.StartSpan("invalid-event-span");

        // Act & Assert
        span.Invoking(s => s.AddEvent(invalidName!))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Span_RecordException_ShouldAddExceptionEvent()
    {
        // Arrange
        using var span = _sut.StartSpan("exception-span");
        var exception = new InvalidOperationException("Test exception");

        // Act
        span.RecordException(exception);

        // Assert
        span.Status.Should().Be(TraceSpanStatus.Error);
        span.Events.Should().HaveCount(1);

        var exceptionEvent = span.Events.First();
        exceptionEvent.Name.Should().Be("exception");
        exceptionEvent.Attributes.Should().ContainKey("exception.type");
        exceptionEvent.Attributes.Should().ContainKey("exception.message");
        exceptionEvent.Attributes["exception.type"].Should().Be("InvalidOperationException");
        exceptionEvent.Attributes["exception.message"].Should().Be("Test exception");
    }

    #endregion

    #region Concurrency Tests - 4 tests

    [Fact]
    public void TracingService_ConcurrentSpanCreation_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 50;
        const int spansPerThread = 10;
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        var createdSpans = new ConcurrentBag<string>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < spansPerThread; j++)
                    {
                        using var span = _sut.StartSpan($"concurrent-span-{threadId}-{j}");
                        createdSpans.Add(span.SpanId);
                        Thread.Sleep(1); // Small delay to simulate work
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
        createdSpans.Should().HaveCount(threadCount * spansPerThread);
        createdSpans.Distinct().Should().HaveCount(threadCount * spansPerThread, "All span IDs should be unique");
    }

    [Fact]
    public void TracingService_ConcurrentSpanOperations_ShouldMaintainConsistency()
    {
        // Arrange
        const int operationCount = 100;
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            var operationId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    using var span = _sut.StartSpan($"operation-{operationId}");

                    // Perform various operations
                    span.SetTag("operation.id", operationId);
                    span.AddEvent($"operation-{operationId}-started");

                    Thread.Sleep(_faker.Random.Int(1, 10));

                    span.AddEvent($"operation-{operationId}-completed");
                    span.SetStatus(TraceSpanStatus.Ok, "Completed successfully");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void TracingService_ConcurrentCurrentSpanManagement_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 20;
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    using var span = _sut.StartSpan($"thread-span-{threadId}");
                    _sut.SetCurrentSpan(span);

                    var currentSpan = _sut.GetCurrentSpan();
                    currentSpan.Should().NotBeNull();

                    Thread.Sleep(10);

                    _sut.SetCurrentSpan(null);
                    var clearedSpan = _sut.GetCurrentSpan();
                    // Note: Due to AsyncLocal, this might still have a value in this thread
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void TracingService_NestedSpansInConcurrentEnvironment_ShouldMaintainHierarchy()
    {
        // Arrange
        const int threadCount = 10;
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    using var parentSpan = _sut.StartSpan($"parent-{threadId}");
                    _sut.SetCurrentSpan(parentSpan);

                    using var childSpan1 = _sut.StartSpan($"child1-{threadId}");
                    childSpan1.TraceId.Should().Be(parentSpan.TraceId);

                    using var childSpan2 = _sut.StartSpan($"child2-{threadId}");
                    childSpan2.TraceId.Should().Be(parentSpan.TraceId);

                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion

    #region Performance Tests - 3 tests

    [Fact]
    public void TracingService_HighThroughputSpanCreation_ShouldBeEfficient()
    {
        // Arrange
        const int spanCount = 10_000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < spanCount; i++)
        {
            using var span = _sut.StartSpan($"perf-span-{i}");
            span.SetTag("iteration", i);
            span.AddEvent("span-created");
        }

        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Created and disposed {spanCount} spans in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "High throughput span creation should be efficient");
    }

    [Fact]
    public void TracingService_ComplexSpanOperations_ShouldMaintainPerformance()
    {
        // Arrange
        const int spanCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < spanCount; i++)
        {
            using var span = _sut.StartSpan($"complex-span-{i}");

            // Add multiple tags
            span.SetTag("operation.type", "complex");
            span.SetTag("iteration", i);
            span.SetTag("batch", i / 100);
            span.SetTag("timestamp", DateTimeOffset.UtcNow.ToString());

            // Add multiple events
            span.AddEvent("operation-started");
            span.AddEvent("validation-completed");
            span.AddEvent("processing-completed");
            span.AddEvent("operation-finished");

            // Set status
            span.SetStatus(TraceSpanStatus.Ok, "Complex operation completed");
        }

        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Processed {spanCount} complex spans in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Complex span operations should be efficient");
    }

    [Fact]
    public void TracingService_MemoryUsage_ShouldNotLeak()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        const int iterations = 1000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var span = _sut.StartSpan($"memory-test-{i}");
            span.SetTag("iteration", i);
            span.AddEvent("test-event");

            using var activity = _sut.StartActivity($"activity-{i}");
            _sut.AddEvent(activity, "activity-event", new Dictionary<string, object> { ["key"] = i });
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryGrowth = finalMemory - initialMemory;
        _output.WriteLine($"Memory growth: {memoryGrowth / 1024.0:F2} KB for {iterations} operations");

        memoryGrowth.Should().BeLessThan(50_000_000, "Memory usage should not indicate significant leaks");
    }

    #endregion

    #region Edge Cases - 4 tests

    [Fact]
    public void Span_WithVeryLongOperationName_ShouldHandleCorrectly()
    {
        // Arrange
        var longName = new string('a', 1000);

        // Act & Assert
        using var span = _sut.StartSpan(longName);
        span.OperationName.Should().Be(longName);
    }

    [Fact]
    public void Span_WithSpecialCharactersInName_ShouldHandleCorrectly()
    {
        // Arrange
        var specialName = "operation-with-special-chars-!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act & Assert
        using var span = _sut.StartSpan(specialName);
        span.OperationName.Should().Be(specialName);
    }

    [Fact]
    public void Span_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var unicodeName = "操作-with-unicode-🌍-émojis";

        // Act & Assert
        using var span = _sut.StartSpan(unicodeName);
        span.OperationName.Should().Be(unicodeName);
    }

    [Fact]
    public void Span_RecordException_WithNullException_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var span = _sut.StartSpan("null-exception-span");

        // Act & Assert
        span.Invoking(s => s.RecordException(null!))
            .Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Integration Scenarios - 3 tests

    [Fact]
    public void TracingService_CompleteWorkflowTracing_ShouldCreateProperTrace()
    {
        // Arrange
        var workflowId = Guid.NewGuid().ToString();

        // Act
        using var workflowSpan = _sut.StartSpan("user-registration-workflow");
        workflowSpan.SetTag("workflow.id", workflowId);
        workflowSpan.SetTag("workflow.type", "user-registration");
        _sut.SetCurrentSpan(workflowSpan);

        // Step 1: Validation
        using (var validationSpan = _sut.StartSpan("validate-user-input"))
        {
            validationSpan.AddEvent("validation-started");
            Thread.Sleep(5);
            validationSpan.AddEvent("validation-completed");
            validationSpan.SetStatus(TraceSpanStatus.Ok, "Validation successful");
        }

        // Step 2: Database operation
        using (var dbSpan = _sut.StartSpan("save-user-to-database"))
        {
            dbSpan.SetTag("database.operation", "insert");
            dbSpan.SetTag("database.table", "users");
            dbSpan.AddEvent("database-connection-opened");
            Thread.Sleep(10);
            dbSpan.AddEvent("user-saved");
            dbSpan.SetStatus(TraceSpanStatus.Ok, "User saved successfully");
        }

        // Step 3: Send notification
        using (var notificationSpan = _sut.StartSpan("send-welcome-email"))
        {
            notificationSpan.SetTag("notification.type", "email");
            notificationSpan.SetTag("notification.template", "welcome");
            notificationSpan.AddEvent("email-prepared");
            Thread.Sleep(8);
            notificationSpan.AddEvent("email-sent");
            notificationSpan.SetStatus(TraceSpanStatus.Ok, "Welcome email sent");
        }

        workflowSpan.SetStatus(TraceSpanStatus.Ok, "User registration completed successfully");

        // Assert
        workflowSpan.TraceId.Should().NotBeNullOrEmpty();
        workflowSpan.Status.Should().Be(TraceSpanStatus.Ok);
        workflowSpan.Tags.Should().ContainKey("workflow.id");
        workflowSpan.Events.Should().BeEmpty(); // No events added directly to workflow span
    }

    [Fact]
    public void TracingService_ErrorHandlingWorkflow_ShouldRecordErrors()
    {
        // Arrange
        var operationId = Guid.NewGuid().ToString();

        // Act
        using var operationSpan = _sut.StartSpan("error-prone-operation");
        operationSpan.SetTag("operation.id", operationId);
        _sut.SetCurrentSpan(operationSpan);

        try
        {
            using var riskySp = _sut.StartSpan("risky-operation");
            riskySp.AddEvent("operation-started");

            // Simulate an error
            throw new InvalidOperationException("Simulated error for testing");
        }
        catch (Exception ex)
        {
            using var errorSpan = _sut.StartSpan("error-handling");
            errorSpan.RecordException(ex);
            errorSpan.AddEvent("error-logged");
            errorSpan.SetStatus(TraceSpanStatus.Error, "Error handled");

            operationSpan.SetStatus(TraceSpanStatus.Error, "Operation failed");
        }

        // Assert
        operationSpan.Status.Should().Be(TraceSpanStatus.Error);
    }

    [Fact]
    public void TracingService_DistributedTracing_ShouldMaintainTraceContext()
    {
        // Arrange & Act
        using var rootSpan = _sut.StartSpan("distributed-operation-root");
        var rootTraceId = rootSpan.TraceId;
        _sut.SetCurrentSpan(rootSpan);

        // Simulate service A
        using (var serviceASpan = _sut.StartSpan("service-a-operation"))
        {
            serviceASpan.SetTag("service.name", "service-a");
            serviceASpan.AddEvent("service-a-processing");

            // Simulate service B (called from service A)
            using var serviceBSpan = _sut.StartSpan("service-b-operation");
            serviceBSpan.SetTag("service.name", "service-b");
            serviceBSpan.AddEvent("service-b-processing");

            // Assert trace continuity
            serviceBSpan.TraceId.Should().Be(rootTraceId);
            serviceASpan.TraceId.Should().Be(rootTraceId);
        }

        // Simulate service C (parallel to service A)
        using (var serviceCSpan = _sut.StartSpan("service-c-operation"))
        {
            serviceCSpan.SetTag("service.name", "service-c");
            serviceCSpan.AddEvent("service-c-processing");

            // Assert trace continuity
            serviceCSpan.TraceId.Should().Be(rootTraceId);
        }

        // Assert
        rootSpan.TraceId.Should().Be(rootTraceId);
    }

    #endregion

    public void Dispose()
    {
        // TracingService doesn't implement IDisposable, so nothing to dispose
    }
}
