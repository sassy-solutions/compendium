// -----------------------------------------------------------------------
// <copyright file="ResultExtensionsTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Results;

public class ResultExtensionsTests
{
    #region Tap Tests

    [Fact]
    public void Tap_OnSuccessResult_ExecutesAction()
    {
        // Arrange
        var value = "test value";
        var result = Result.Success(value);
        var executed = false;
        string? capturedValue = null;

        // Act
        var returnedResult = result.Tap(v =>
        {
            executed = true;
            capturedValue = v;
        });

        // Assert
        executed.Should().BeTrue();
        capturedValue.Should().Be(value);
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void Tap_OnFailureResult_DoesNotExecuteAction()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);
        var executed = false;

        // Act
        var returnedResult = result.Tap(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void Tap_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<string>)null!).Tap(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Tap_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Success("test");

        // Act
        var act = () => result.Tap(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region TapError Tests

    [Fact]
    public void TapError_OnFailureResult_ExecutesAction()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);
        var executed = false;
        Error? capturedError = null;

        // Act
        var returnedResult = result.TapError(e =>
        {
            executed = true;
            capturedError = e;
        });

        // Assert
        executed.Should().BeTrue();
        capturedError.Should().Be(error);
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void TapError_OnSuccessResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result.Success("test value");
        var executed = false;

        // Act
        var returnedResult = result.TapError(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void TapError_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<string>)null!).TapError(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TapError_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Failure<string>(TestData.Errors.CreateValidation());

        // Act
        var act = () => result.TapError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Map Tests

    [Fact]
    public void Map_OnSuccessResult_TransformsValue()
    {
        // Arrange
        var originalValue = "test";
        var result = Result.Success(originalValue);

        // Act
        var mappedResult = result.Map(v => v.Length);

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be(4);
    }

    [Fact]
    public void Map_OnFailureResult_PreservesError()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);

        // Act
        var mappedResult = result.Map(v => v.Length);

        // Assert
        mappedResult.IsFailure.Should().BeTrue();
        mappedResult.Error.Should().Be(error);
    }

    [Fact]
    public void Map_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<string>)null!).Map(v => v.Length);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_WithNullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Success("test");

        // Act
        var act = () => result.Map<string, int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_FromResult_OnSuccess_ReturnsSuccessWithValue()
    {
        // Arrange
        var result = Result.Success();
        var value = "mapped value";

        // Act
        var mappedResult = result.Map(value);

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be(value);
    }

    [Fact]
    public void Map_FromResult_OnFailure_ReturnsFailure()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure(error);
        var value = "mapped value";

        // Act
        var mappedResult = result.Map(value);

        // Assert
        mappedResult.IsFailure.Should().BeTrue();
        mappedResult.Error.Should().Be(error);
    }

    [Fact]
    public void Map_FromResult_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result)null!).Map("value");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Bind Tests

    [Fact]
    public void Bind_OnSuccessResult_ExecutesBinder()
    {
        // Arrange
        var originalValue = "test";
        var result = Result.Success(originalValue);

        // Act
        var boundResult = result.Bind(v => Result.Success(v.Length));

        // Assert
        boundResult.IsSuccess.Should().BeTrue();
        boundResult.Value.Should().Be(4);
    }

    [Fact]
    public void Bind_OnSuccessResult_BinderReturnsFailure_ReturnsFailure()
    {
        // Arrange
        var originalValue = "test";
        var result = Result.Success(originalValue);
        var binderError = TestData.Errors.CreateValidation("BIND_ERR", "Binder error");

        // Act
        var boundResult = result.Bind<string, int>(_ => Result.Failure<int>(binderError));

        // Assert
        boundResult.IsFailure.Should().BeTrue();
        boundResult.Error.Should().Be(binderError);
    }

    [Fact]
    public void Bind_OnFailureResult_DoesNotExecuteBinder()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);
        var binderExecuted = false;

        // Act
        var boundResult = result.Bind<string, int>(v =>
        {
            binderExecuted = true;
            return Result.Success(v.Length);
        });

        // Assert
        binderExecuted.Should().BeFalse();
        boundResult.IsFailure.Should().BeTrue();
        boundResult.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<string>)null!).Bind(v => Result.Success(v.Length));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Bind_WithNullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Success("test");

        // Act
        var act = () => result.Bind<string, int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Match Tests

    [Fact]
    public void Match_OnSuccessResult_ExecutesOnSuccess()
    {
        // Arrange
        var value = "test value";
        var result = Result.Success(value);

        // Act
        var matchResult = result.Match(
            onSuccess: v => $"Success: {v}",
            onFailure: e => $"Failure: {e.Message}");

        // Assert
        matchResult.Should().Be("Success: test value");
    }

    [Fact]
    public void Match_OnFailureResult_ExecutesOnFailure()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation("ERR001", "Test error");
        var result = Result.Failure<string>(error);

        // Act
        var matchResult = result.Match(
            onSuccess: v => $"Success: {v}",
            onFailure: e => $"Failure: {e.Message}");

        // Assert
        matchResult.Should().Be("Failure: Test error");
    }

    [Fact]
    public void Match_WithActions_OnSuccessResult_ExecutesOnSuccess()
    {
        // Arrange
        var value = "test value";
        var result = Result.Success(value);
        var successExecuted = false;
        var failureExecuted = false;
        string? capturedValue = null;

        // Act
        result.Match(
            onSuccess: v =>
            {
                successExecuted = true;
                capturedValue = v;
            },
            onFailure: _ => failureExecuted = true);

        // Assert
        successExecuted.Should().BeTrue();
        failureExecuted.Should().BeFalse();
        capturedValue.Should().Be(value);
    }

    [Fact]
    public void Match_WithActions_OnFailureResult_ExecutesOnFailure()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);
        var successExecuted = false;
        var failureExecuted = false;
        Error? capturedError = null;

        // Act
        result.Match(
            onSuccess: _ => successExecuted = true,
            onFailure: e =>
            {
                failureExecuted = true;
                capturedError = e;
            });

        // Assert
        successExecuted.Should().BeFalse();
        failureExecuted.Should().BeTrue();
        capturedError.Should().Be(error);
    }

    [Fact]
    public void Match_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<string>)null!).Match(v => v, e => e.Message);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Match_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Success("test");

        // Act
        var act = () => result.Match<string, string>(null!, e => e.Message);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Match_WithNullOnFailure_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Success("test");

        // Act
        var act = () => result.Match(v => v, (Func<Error, string>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task MapAsync_OnSuccessResult_TransformsValue()
    {
        // Arrange
        var originalValue = "test";
        var resultTask = Task.FromResult(Result.Success(originalValue));

        // Act
        var mappedResult = await resultTask.MapAsync(v => v.Length);

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be(4);
    }

    [Fact]
    public async Task MapAsync_OnFailureResult_PreservesError()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var resultTask = Task.FromResult(Result.Failure<string>(error));

        // Act
        var mappedResult = await resultTask.MapAsync(v => v.Length);

        // Assert
        mappedResult.IsFailure.Should().BeTrue();
        mappedResult.Error.Should().Be(error);
    }

    [Fact]
    public async Task MapAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await ((Task<Result<string>>)null!).MapAsync(v => v.Length);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MapAsync_WithNullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success("test"));

        // Act
        var act = async () => await resultTask.MapAsync<string, int>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BindAsync_OnSuccessResult_ExecutesBinder()
    {
        // Arrange
        var originalValue = "test";
        var resultTask = Task.FromResult(Result.Success(originalValue));

        // Act
        var boundResult = await resultTask.BindAsync(v => Task.FromResult(Result.Success(v.Length)));

        // Assert
        boundResult.IsSuccess.Should().BeTrue();
        boundResult.Value.Should().Be(4);
    }

    [Fact]
    public async Task BindAsync_OnFailureResult_DoesNotExecuteBinder()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var resultTask = Task.FromResult(Result.Failure<string>(error));
        var binderExecuted = false;

        // Act
        var boundResult = await resultTask.BindAsync(async v =>
        {
            binderExecuted = true;
            return await Task.FromResult(Result.Success(v.Length));
        });

        // Assert
        binderExecuted.Should().BeFalse();
        boundResult.IsFailure.Should().BeTrue();
        boundResult.Error.Should().Be(error);
    }

    [Fact]
    public async Task BindAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await ((Task<Result<string>>)null!).BindAsync(v => Task.FromResult(Result.Success(v.Length)));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BindAsync_WithNullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success("test"));

        // Act
        var act = async () => await resultTask.BindAsync<string, int>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TapAsync_OnSuccessResult_ExecutesAction()
    {
        // Arrange
        var value = "test value";
        var resultTask = Task.FromResult(Result.Success(value));
        var executed = false;
        string? capturedValue = null;

        // Act
        var returnedResult = await resultTask.TapAsync(async v =>
        {
            executed = true;
            capturedValue = v;
            await Task.Delay(1); // Simulate async work
        });

        // Assert
        executed.Should().BeTrue();
        capturedValue.Should().Be(value);
        returnedResult.IsSuccess.Should().BeTrue();
        returnedResult.Value.Should().Be(value);
    }

    [Fact]
    public async Task TapAsync_OnFailureResult_DoesNotExecuteAction()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var resultTask = Task.FromResult(Result.Failure<string>(error));
        var executed = false;

        // Act
        var returnedResult = await resultTask.TapAsync(async _ =>
        {
            executed = true;
            await Task.Delay(1);
        });

        // Assert
        executed.Should().BeFalse();
        returnedResult.IsFailure.Should().BeTrue();
        returnedResult.Error.Should().Be(error);
    }

    [Fact]
    public async Task TapAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await ((Task<Result<string>>)null!).TapAsync(_ => Task.CompletedTask);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TapAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success("test"));

        // Act
        var act = async () => await resultTask.TapAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Performance Tests

    [Theory]
    [InlineData(1000)]
    public void ResultExtensions_Performance_Test(int iterations)
    {
        // Arrange
        var result = Result.Success("test value");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = result.Map(v => v.Length)
                     .Bind(len => Result.Success(len * 2))
                     .Tap(val => { /* side effect */ })
                     .Match(
                         onSuccess: val => val.ToString(),
                         onFailure: err => err.Message);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Result extensions should be fast");
    }

    [Fact]
    public void ResultExtensions_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var result = Result.Success("test value");
        var results = new List<string>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, i =>
        {
            var transformed = result.Map(v => $"{v}_{i}")
                                   .Match(
                                       onSuccess: v => v,
                                       onFailure: e => e.Message);

            lock (lockObject)
            {
                results.Add(transformed);
            }
        });

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().StartWith("test value_"));
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void ResultExtensions_Chaining_Success_Pipeline()
    {
        // Arrange
        var input = "hello world";
        var result = Result.Success(input);

        // Act
        var finalResult = result
            .Map(s => s.ToUpper())
            .Bind(s => Result.Success(s.Replace(" ", "_")))
            .Tap(s => s.Should().Be("HELLO_WORLD"))
            .Map(s => s.Length);

        // Assert
        finalResult.IsSuccess.Should().BeTrue();
        finalResult.Value.Should().Be(11);
    }

    [Fact]
    public void ResultExtensions_Chaining_Failure_ShortCircuits()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);
        var mapExecuted = false;
        var bindExecuted = false;
        var tapExecuted = false;

        // Act
        var finalResult = result
            .Map(s =>
            {
                mapExecuted = true;
                return s.ToUpper();
            })
            .Bind(s =>
            {
                bindExecuted = true;
                return Result.Success(s.Replace(" ", "_"));
            })
            .Tap(s => tapExecuted = true);

        // Assert
        finalResult.IsFailure.Should().BeTrue();
        finalResult.Error.Should().Be(error);
        mapExecuted.Should().BeFalse();
        bindExecuted.Should().BeFalse();
        tapExecuted.Should().BeFalse();
    }

    #endregion
}
