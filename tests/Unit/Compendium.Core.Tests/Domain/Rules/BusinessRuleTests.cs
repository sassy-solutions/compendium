// -----------------------------------------------------------------------
// <copyright file="BusinessRuleTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Domain.Rules;

public class BusinessRuleTests
{
    #region Test Business Rules

    private class ValidBusinessRule : IBusinessRule
    {
        public string Message => "This rule is always valid";
        public string ErrorCode => "VALID.001";
        public bool IsBroken() => false;
    }

    private class BrokenBusinessRule : IBusinessRule
    {
        public string Message => "This rule is always broken";
        public string ErrorCode => "BROKEN.001";
        public bool IsBroken() => true;
    }

    private class ConditionalBusinessRule : IBusinessRule
    {
        private readonly bool _isBroken;

        public ConditionalBusinessRule(bool isBroken, string message = "Conditional rule", string errorCode = "COND.001")
        {
            _isBroken = isBroken;
            Message = message;
            ErrorCode = errorCode;
        }

        public string Message { get; }
        public string ErrorCode { get; }
        public bool IsBroken() => _isBroken;
    }

    private class ParameterizedBusinessRule : IBusinessRule
    {
        private readonly string _value;
        private readonly int _minLength;

        public ParameterizedBusinessRule(string value, int minLength)
        {
            _value = value;
            _minLength = minLength;
        }

        public string Message => $"Value '{_value}' must be at least {_minLength} characters long";
        public string ErrorCode => "PARAM.001";
        public bool IsBroken() => string.IsNullOrEmpty(_value) || _value.Length < _minLength;
    }

    private class NullMessageBusinessRule : IBusinessRule
    {
        public string Message => null!;
        public string ErrorCode => "NULL.001";
        public bool IsBroken() => true;
    }

    private class EmptyMessageBusinessRule : IBusinessRule
    {
        public string Message => string.Empty;
        public string ErrorCode => "EMPTY.001";
        public bool IsBroken() => true;
    }

    private class NullErrorCodeBusinessRule : IBusinessRule
    {
        public string Message => "Test message";
        public string ErrorCode => null!;
        public bool IsBroken() => true;
    }

    #endregion

    #region IBusinessRule Interface Tests

    [Fact]
    public void ValidBusinessRule_IsBroken_ReturnsFalse()
    {
        // Arrange
        var rule = new ValidBusinessRule();

        // Act
        var isBroken = rule.IsBroken();

        // Assert
        isBroken.Should().BeFalse();
    }

    [Fact]
    public void BrokenBusinessRule_IsBroken_ReturnsTrue()
    {
        // Arrange
        var rule = new BrokenBusinessRule();

        // Act
        var isBroken = rule.IsBroken();

        // Assert
        isBroken.Should().BeTrue();
    }

    [Fact]
    public void ValidBusinessRule_HasCorrectProperties()
    {
        // Arrange
        var rule = new ValidBusinessRule();

        // Act & Assert
        rule.Message.Should().Be("This rule is always valid");
        rule.ErrorCode.Should().Be("VALID.001");
    }

    [Fact]
    public void BrokenBusinessRule_HasCorrectProperties()
    {
        // Arrange
        var rule = new BrokenBusinessRule();

        // Act & Assert
        rule.Message.Should().Be("This rule is always broken");
        rule.ErrorCode.Should().Be("BROKEN.001");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConditionalBusinessRule_IsBroken_ReturnsExpectedValue(bool expectedBroken)
    {
        // Arrange
        var rule = new ConditionalBusinessRule(expectedBroken);

        // Act
        var isBroken = rule.IsBroken();

        // Assert
        isBroken.Should().Be(expectedBroken);
    }

    [Theory]
    [InlineData("test", 3, false)] // Valid: length >= minLength
    [InlineData("test", 4, false)] // Valid: length == minLength
    [InlineData("test", 5, true)]  // Invalid: length < minLength
    [InlineData("", 1, true)]      // Invalid: empty string
    [InlineData(null, 1, true)]    // Invalid: null string
    public void ParameterizedBusinessRule_IsBroken_ReturnsExpectedValue(string? value, int minLength, bool expectedBroken)
    {
        // Arrange
        var rule = new ParameterizedBusinessRule(value!, minLength);

        // Act
        var isBroken = rule.IsBroken();

        // Assert
        isBroken.Should().Be(expectedBroken);
    }

    [Fact]
    public void ParameterizedBusinessRule_Message_IncludesParameters()
    {
        // Arrange
        var value = "test";
        var minLength = 5;
        var rule = new ParameterizedBusinessRule(value, minLength);

        // Act
        var message = rule.Message;

        // Assert
        message.Should().Contain(value);
        message.Should().Contain(minLength.ToString());
        message.Should().Be("Value 'test' must be at least 5 characters long");
    }

    #endregion

    #region BusinessRuleValidationException Tests

    [Fact]
    public void BusinessRuleValidationException_WithValidRule_SetsPropertiesCorrectly()
    {
        // Arrange
        var rule = new BrokenBusinessRule();

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.BrokenRule.Should().Be(rule);
        exception.ErrorCode.Should().Be(rule.ErrorCode);
        exception.Message.Should().Be(rule.Message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void BusinessRuleValidationException_WithInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var rule = new BrokenBusinessRule();
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new BusinessRuleValidationException(rule, innerException);

        // Assert
        exception.BrokenRule.Should().Be(rule);
        exception.ErrorCode.Should().Be(rule.ErrorCode);
        exception.Message.Should().Be(rule.Message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void BusinessRuleValidationException_WithNullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BusinessRuleValidationException(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("brokenRule");
    }

    [Fact]
    public void BusinessRuleValidationException_WithNullRuleAndInnerException_ThrowsArgumentNullException()
    {
        // Arrange
        var innerException = new InvalidOperationException();

        // Act
        var act = () => new BusinessRuleValidationException(null!, innerException);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("brokenRule");
    }

    [Fact]
    public void BusinessRuleValidationException_WithNullMessage_UsesDefaultMessage()
    {
        // Arrange
        var rule = new NullMessageBusinessRule();

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.Message.Should().Be("A business rule was violated.");
        exception.BrokenRule.Should().Be(rule);
        exception.ErrorCode.Should().Be(rule.ErrorCode);
    }

    [Fact]
    public void BusinessRuleValidationException_WithEmptyMessage_UsesEmptyMessage()
    {
        // Arrange
        var rule = new EmptyMessageBusinessRule();

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.Message.Should().Be(string.Empty);
        exception.BrokenRule.Should().Be(rule);
        exception.ErrorCode.Should().Be(rule.ErrorCode);
    }

    [Fact]
    public void BusinessRuleValidationException_WithNullErrorCode_UsesNullErrorCode()
    {
        // Arrange
        var rule = new NullErrorCodeBusinessRule();

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.ErrorCode.Should().BeNull();
        exception.BrokenRule.Should().Be(rule);
        exception.Message.Should().Be(rule.Message);
    }

    [Fact]
    public void BusinessRuleValidationException_ToString_ReturnsFormattedString()
    {
        // Arrange
        var rule = new BrokenBusinessRule();
        var exception = new BusinessRuleValidationException(rule);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("BusinessRuleValidationException");
        result.Should().Contain(rule.Message);
        result.Should().Contain(rule.ErrorCode);
        result.Should().Be($"BusinessRuleValidationException: {rule.Message} (ErrorCode: {rule.ErrorCode})");
    }

    [Fact]
    public void BusinessRuleValidationException_ToString_WithNullErrorCode_HandlesGracefully()
    {
        // Arrange
        var rule = new NullErrorCodeBusinessRule();
        var exception = new BusinessRuleValidationException(rule);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("BusinessRuleValidationException");
        result.Should().Contain(rule.Message);
        result.Should().Contain("(ErrorCode: )");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Entity_CheckRule_WithValidRule_DoesNotThrow()
    {
        // Arrange
        var entity = TestData.Entities.CreateValid();
        var rule = new ValidBusinessRule();

        // Act
        var act = () => entity.TestCheckRule(rule);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Entity_CheckRule_WithBrokenRule_ThrowsBusinessRuleValidationException()
    {
        // Arrange
        var entity = TestData.Entities.CreateValid();
        var rule = new BrokenBusinessRule();

        // Act
        var act = () => entity.TestCheckRule(rule);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
           .Which.BrokenRule.Should().Be(rule);
    }

    [Fact]
    public void Entity_CheckRule_WithParameterizedRule_WorksCorrectly()
    {
        // Arrange
        var entity = TestData.Entities.CreateValid();
        var validRule = new ParameterizedBusinessRule("valid", 3);
        var invalidRule = new ParameterizedBusinessRule("no", 5);

        // Act & Assert
        entity.Invoking(e => e.TestCheckRule(validRule)).Should().NotThrow();
        entity.Invoking(e => e.TestCheckRule(invalidRule)).Should().Throw<BusinessRuleValidationException>();
    }

    #endregion

    #region Performance Tests

    [Theory]
    [InlineData(1000)]
    public void BusinessRule_Evaluation_PerformanceTest(int iterations)
    {
        // Arrange
        var rules = new IBusinessRule[]
        {
            new ValidBusinessRule(),
            new BrokenBusinessRule(),
            new ConditionalBusinessRule(false),
            new ParameterizedBusinessRule("test", 3)
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            foreach (var rule in rules)
            {
                _ = rule.IsBroken();
                _ = rule.Message;
                _ = rule.ErrorCode;
            }
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Business rule evaluation should be fast");
    }

    [Fact]
    public void BusinessRule_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var rule = new ParameterizedBusinessRule("test", 3);
        var results = new List<bool>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, i =>
        {
            var isBroken = rule.IsBroken();
            var message = rule.Message;
            var errorCode = rule.ErrorCode;

            lock (lockObject)
            {
                results.Add(isBroken);
            }

            // Verify properties are consistent
            message.Should().NotBeNullOrEmpty();
            errorCode.Should().NotBeNullOrEmpty();
        });

        // Assert
        results.Should().HaveCount(100);
        results.Should().OnlyContain(r => r == false); // All should be false for this rule
    }

    [Theory]
    [InlineData(100)]
    public void BusinessRuleValidationException_Creation_PerformanceTest(int iterations)
    {
        // Arrange
        var rule = new BrokenBusinessRule();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var exception = new BusinessRuleValidationException(rule);
            _ = exception.Message;
            _ = exception.ErrorCode;
            _ = exception.BrokenRule;
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Exception creation should be fast");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void BusinessRule_WithVeryLongMessage_HandlesCorrectly()
    {
        // Arrange
        var longMessage = new string('M', 10000);
        var rule = new ConditionalBusinessRule(true, longMessage, "LONG.001");

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.Message.Should().Be(longMessage);
        exception.ErrorCode.Should().Be("LONG.001");
    }

    [Fact]
    public void BusinessRule_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var message = "Message with special chars: ñáéíóú@#$%^&*()[]{}";
        var errorCode = "SPECIAL.001.ñ@#$%";
        var rule = new ConditionalBusinessRule(true, message, errorCode);

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void BusinessRule_WithEmptyStrings_HandlesCorrectly()
    {
        // Arrange
        var rule = new ConditionalBusinessRule(true, string.Empty, string.Empty);

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.Message.Should().Be(string.Empty);
        exception.ErrorCode.Should().Be(string.Empty);
    }

    #endregion

    #region Complex Business Rule Examples

    private class EmailValidationRule : IBusinessRule
    {
        private readonly string _email;

        public EmailValidationRule(string email)
        {
            _email = email;
        }

        public string Message => $"Email '{_email}' is not in a valid format";
        public string ErrorCode => "EMAIL.INVALID";

        public bool IsBroken()
        {
            if (string.IsNullOrWhiteSpace(_email))
            {
                return true;
            }

            var atIndex = _email.IndexOf('@');
            if (atIndex <= 0 || atIndex == _email.Length - 1)
            {
                return true;
            }

            var dotIndex = _email.LastIndexOf('.');
            return dotIndex <= atIndex || dotIndex == _email.Length - 1;
        }
    }

    private class AgeValidationRule(int age, int minAge = 0, int maxAge = 150) : IBusinessRule
    {
        public string Message => $"Age {age} must be between {minAge} and {maxAge}";
        public string ErrorCode => "AGE.INVALID";

        public bool IsBroken() => age < minAge || age > maxAge;
    }

    [Theory]
    [InlineData("test@example.com", false)]
    [InlineData("invalid-email", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("test@", true)]
    [InlineData("@example.com", true)]
    public void EmailValidationRule_IsBroken_ReturnsExpectedValue(string? email, bool expectedBroken)
    {
        // Arrange
        var rule = new EmailValidationRule(email!);

        // Act
        var isBroken = rule.IsBroken();

        // Assert
        isBroken.Should().Be(expectedBroken);
    }

    [Theory]
    [InlineData(25, 18, 65, false)]  // Valid age
    [InlineData(17, 18, 65, true)]   // Too young
    [InlineData(66, 18, 65, true)]   // Too old
    [InlineData(18, 18, 65, false)]  // Minimum age
    [InlineData(65, 18, 65, false)]  // Maximum age
    public void AgeValidationRule_IsBroken_ReturnsExpectedValue(int age, int minAge, int maxAge, bool expectedBroken)
    {
        // Arrange
        var rule = new AgeValidationRule(age, minAge, maxAge);

        // Act
        var isBroken = rule.IsBroken();

        // Assert
        isBroken.Should().Be(expectedBroken);
    }

    #endregion
}
