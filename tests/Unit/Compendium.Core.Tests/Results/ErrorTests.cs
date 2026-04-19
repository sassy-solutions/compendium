// -----------------------------------------------------------------------
// <copyright file="ErrorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Results;

public class ErrorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var code = "TEST.001";
        var message = "Test error message";
        var type = ErrorType.Validation;
        var metadata = new Dictionary<string, object> { { "field", "username" } };

        // Act
        var error = new Error(code, message, type, metadata);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(type);
        error.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Constructor_WithNullCode_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Error(null!, "message", ErrorType.Failure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("code");
    }

    [Fact]
    public void Constructor_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Error("code", null!, ErrorType.Failure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("message");
    }

    [Fact]
    public void Constructor_WithNullMetadata_UsesEmptyDictionary()
    {
        // Act
        var error = new Error("code", "message", ErrorType.Failure, null);

        // Assert
        error.Metadata.Should().NotBeNull();
        error.Metadata.Should().BeEmpty();
    }

    #endregion

    #region Static Properties Tests

    [Fact]
    public void None_HasCorrectProperties()
    {
        // Act
        var error = Error.None;

        // Assert
        error.Code.Should().Be(string.Empty);
        error.Message.Should().Be(string.Empty);
        error.Type.Should().Be(ErrorType.None);
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void NullValue_HasCorrectProperties()
    {
        // Act
        var error = Error.NullValue;

        // Assert
        error.Code.Should().Be("Error.NullValue");
        error.Message.Should().Be("The specified result value is null.");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().BeEmpty();
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Failure_CreatesFailureError()
    {
        // Arrange
        var code = "FAIL.001";
        var message = "Failure message";
        var metadata = new Dictionary<string, object> { { "context", "test" } };

        // Act
        var error = Error.Failure(code, message, metadata);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Validation_CreatesValidationError()
    {
        // Arrange
        var code = "VAL.001";
        var message = "Validation message";

        // Act
        var error = Error.Validation(code, message);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Validation);
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void NotFound_CreatesNotFoundError()
    {
        // Arrange
        var code = "NF.001";
        var message = "Not found message";

        // Act
        var error = Error.NotFound(code, message);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.NotFound);
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Conflict_CreatesConflictError()
    {
        // Arrange
        var code = "CONF.001";
        var message = "Conflict message";

        // Act
        var error = Error.Conflict(code, message);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Conflict);
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Unauthorized_CreatesUnauthorizedError()
    {
        // Arrange
        var code = "UNAUTH.001";
        var message = "Unauthorized message";

        // Act
        var error = Error.Unauthorized(code, message);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Forbidden_CreatesForbiddenError()
    {
        // Arrange
        var code = "FORB.001";
        var message = "Forbidden message";

        // Act
        var error = Error.Forbidden(code, message);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Metadata.Should().BeEmpty();
    }

    [Theory]
    [InlineData("FAIL.001", "Failure message", ErrorType.Failure)]
    [InlineData("VAL.002", "Validation message", ErrorType.Validation)]
    [InlineData("NF.003", "Not found message", ErrorType.NotFound)]
    [InlineData("CONF.004", "Conflict message", ErrorType.Conflict)]
    [InlineData("UNAUTH.005", "Unauthorized message", ErrorType.Unauthorized)]
    [InlineData("FORB.006", "Forbidden message", ErrorType.Forbidden)]
    public void FactoryMethods_WithMetadata_SetsMetadataCorrectly(string code, string message, ErrorType type)
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "field", "testField" },
            { "value", 42 },
            { "timestamp", DateTime.UtcNow }
        };

        // Act
        var error = type switch
        {
            ErrorType.Failure => Error.Failure(code, message, metadata),
            ErrorType.Validation => Error.Validation(code, message, metadata),
            ErrorType.NotFound => Error.NotFound(code, message, metadata),
            ErrorType.Conflict => Error.Conflict(code, message, metadata),
            ErrorType.Unauthorized => Error.Unauthorized(code, message, metadata),
            ErrorType.Forbidden => Error.Forbidden(code, message, metadata),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Type.Should().Be(type);
        error.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_FromString_CreatesFailureError()
    {
        // Arrange
        var message = "Test error message";

        // Act
        Error error = message;

        // Assert
        error.Code.Should().Be("General.Failure");
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void ImplicitOperator_FromEmptyString_CreatesFailureError()
    {
        // Arrange
        var message = string.Empty;

        // Act
        Error error = message;

        // Assert
        error.Code.Should().Be("General.Failure");
        error.Message.Should().Be(message);
        error.Type.Should().Be(ErrorType.Failure);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameCodeMessageAndType_ReturnsTrue()
    {
        // Arrange
        var error1 = Error.Validation("VAL.001", "Test message");
        var error2 = Error.Validation("VAL.001", "Test message");

        // Act & Assert
        error1.Should().Be(error2);
        (error1 == error2).Should().BeTrue();
        (error1 != error2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentCode_ReturnsFalse()
    {
        // Arrange
        var error1 = Error.Validation("VAL.001", "Test message");
        var error2 = Error.Validation("VAL.002", "Test message");

        // Act & Assert
        error1.Should().NotBe(error2);
        (error1 == error2).Should().BeFalse();
        (error1 != error2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        // Arrange
        var error1 = Error.Validation("VAL.001", "Test message 1");
        var error2 = Error.Validation("VAL.001", "Test message 2");

        // Act & Assert
        error1.Should().NotBe(error2);
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var error1 = Error.Validation("VAL.001", "Test message");
        var error2 = Error.Failure("VAL.001", "Test message");

        // Act & Assert
        error1.Should().NotBe(error2);
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var error = Error.Validation("VAL.001", "Test message");

        // Act & Assert
        error.Should().Be(error);
#pragma warning disable CS1718 // Comparison made to same variable - intentional for reference equality test
        (error == error).Should().BeTrue();
#pragma warning restore CS1718
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var error = Error.Validation("VAL.001", "Test message");

        // Act & Assert
        error.Should().NotBe(null);
        (error == null).Should().BeFalse();
        (null == error).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameErrors_ReturnsSameHash()
    {
        // Arrange
        var error1 = Error.Validation("VAL.001", "Test message");
        var error2 = Error.Validation("VAL.001", "Test message");

        // Act
        var hash1 = error1.GetHashCode();
        var hash2 = error2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_DifferentErrors_ReturnsDifferentHash()
    {
        // Arrange
        var error1 = Error.Validation("VAL.001", "Test message");
        var error2 = Error.Validation("VAL.002", "Test message");

        // Act
        var hash1 = error1.GetHashCode();
        var hash2 = error2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var error = Error.Validation("VAL.001", "Test validation error");

        // Act
        var result = error.ToString();

        // Assert
        result.Should().Be("Error [Code=VAL.001, Message=Test validation error, Type=Validation]");
    }

    [Fact]
    public void ToString_WithEmptyCodeAndMessage_ReturnsFormattedString()
    {
        // Arrange
        var error = Error.None;

        // Act
        var result = error.ToString();

        // Assert
        result.Should().Be("Error [Code=, Message=, Type=None]");
    }

    [Theory]
    [InlineData(ErrorType.None)]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void ToString_WithDifferentTypes_IncludesTypeInString(ErrorType errorType)
    {
        // Arrange
        var error = new Error("TEST.001", "Test message", errorType);

        // Act
        var result = error.ToString();

        // Assert
        result.Should().Contain($"Type={errorType}");
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void Metadata_WithComplexObjects_StoresCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "string", "value" },
            { "int", 42 },
            { "bool", true },
            { "datetime", DateTime.UtcNow },
            { "list", new List<string> { "item1", "item2" } },
            { "nested", new { Property = "value" } }
        };

        // Act
        var error = Error.Validation("VAL.001", "Test message", metadata);

        // Assert
        error.Metadata.Should().BeEquivalentTo(metadata);
        error.Metadata["string"].Should().Be("value");
        error.Metadata["int"].Should().Be(42);
        error.Metadata["bool"].Should().Be(true);
    }

    [Fact]
    public void Metadata_IsReadOnly_CannotBeModified()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var error = Error.Validation("VAL.001", "Test message", metadata);

        // Act & Assert
        error.Metadata.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>();

        // Verify we can't cast to mutable dictionary and modify
        var readOnlyMetadata = error.Metadata;
        readOnlyMetadata.Should().ContainKey("key");
        readOnlyMetadata["key"].Should().Be("value");
    }

    #endregion

    #region Performance Tests

    [Theory]
    [InlineData(1000)]
    public void Error_Creation_PerformanceTest(int iterations)
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = Error.Validation($"VAL.{i:000}", $"Validation error {i}");
            _ = Error.Failure($"FAIL.{i:000}", $"Failure error {i}");
            _ = Error.NotFound($"NF.{i:000}", $"Not found error {i}");
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Error creation should be fast");
    }

    [Fact]
    public void Error_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var errors = new List<Error>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, i =>
        {
            var error = Error.Validation($"VAL.{i:000}", $"Validation error {i}");
            lock (lockObject)
            {
                errors.Add(error);
            }
        });

        // Assert
        errors.Should().HaveCount(100);
        errors.Should().OnlyContain(e => e.Type == ErrorType.Validation);
        errors.Select(e => e.Code).Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Error_WithVeryLongCode_HandlesCorrectly()
    {
        // Arrange
        var longCode = new string('A', 1000);
        var message = "Test message";

        // Act
        var error = Error.Validation(longCode, message);

        // Assert
        error.Code.Should().Be(longCode);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Error_WithVeryLongMessage_HandlesCorrectly()
    {
        // Arrange
        var code = "VAL.001";
        var longMessage = new string('M', 10000);

        // Act
        var error = Error.Validation(code, longMessage);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(longMessage);
    }

    [Fact]
    public void Error_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var code = "VAL.001.ñ@#$%^&*()";
        var message = "Message with special chars: ñáéíóú@#$%^&*()[]{}";

        // Act
        var error = Error.Validation(code, message);

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void Error_WithEmptyStrings_HandlesCorrectly()
    {
        // Arrange
        var code = string.Empty;
        var message = string.Empty;

        // Act
        var error = new Error(code, message, ErrorType.Validation);

        // Assert
        error.Code.Should().Be(string.Empty);
        error.Message.Should().Be(string.Empty);
        error.Type.Should().Be(ErrorType.Validation);
    }

    #endregion

    #region ErrorType Enum Tests

    [Fact]
    public void ErrorType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ErrorType>().Should().BeEquivalentTo(new[]
        {
            ErrorType.None,
            ErrorType.Failure,
            ErrorType.Validation,
            ErrorType.NotFound,
            ErrorType.Conflict,
            ErrorType.Unauthorized,
            ErrorType.Forbidden,
            ErrorType.Unavailable,
            ErrorType.TooManyRequests,
            ErrorType.Unexpected
        });
    }

    [Fact]
    public void ErrorType_HasCorrectNumericValues()
    {
        // Assert
        ((int)ErrorType.None).Should().Be(0);
        ((int)ErrorType.Failure).Should().Be(1);
        ((int)ErrorType.Validation).Should().Be(2);
        ((int)ErrorType.NotFound).Should().Be(3);
        ((int)ErrorType.Conflict).Should().Be(4);
        ((int)ErrorType.Unauthorized).Should().Be(5);
        ((int)ErrorType.Forbidden).Should().Be(6);
    }

    #endregion

    #region Integration with TestData

    [Fact]
    public void TestData_Errors_CreateValidation_WorksCorrectly()
    {
        // Act
        var error = TestData.Errors.CreateValidation();

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().NotBeNullOrEmpty();
        error.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TestData_Errors_CreateValidation_WithParameters_WorksCorrectly()
    {
        // Arrange
        var code = "CUSTOM.001";
        var message = "Custom validation message";

        // Act
        var error = TestData.Errors.CreateValidation(code, message);

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
    }

    #endregion
}
