// -----------------------------------------------------------------------
// <copyright file="JobsErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Jobs.Tests;

public class JobsErrorsTests
{
    [Fact]
    public void JobNotFound_WithJobId_ReturnsNotFoundError()
    {
        // Arrange
        const string jobId = "job-123";

        // Act
        var error = JobsErrors.JobNotFound(jobId);

        // Assert
        error.Code.Should().Be("Jobs.JobNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain($"'{jobId}'");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("very-long-job-identifier-2026-05-10-abc-def")]
    public void JobNotFound_WithVariousIds_EmbedsIdInMessage(string jobId)
    {
        // Act
        var error = JobsErrors.JobNotFound(jobId);

        // Assert
        error.Code.Should().Be("Jobs.JobNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain($"'{jobId}'");
    }

    [Fact]
    public void InvalidPayload_WithReason_ReturnsValidationError()
    {
        // Arrange
        const string reason = "payload exceeds 1MB limit";

        // Act
        var error = JobsErrors.InvalidPayload(reason);

        // Assert
        error.Code.Should().Be("Jobs.InvalidPayload");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void CronInvalid_WithExpression_ReturnsValidationError()
    {
        // Arrange
        const string cron = "not-a-cron";

        // Act
        var error = JobsErrors.CronInvalid(cron);

        // Assert
        error.Code.Should().Be("Jobs.CronInvalid");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain($"'{cron}'");
    }

    [Fact]
    public void RateLimited_WithReason_ReturnsTooManyRequestsError()
    {
        // Arrange
        const string reason = "100 enqueues/sec exceeded";

        // Act
        var error = JobsErrors.RateLimited(reason);

        // Assert
        error.Code.Should().Be("Jobs.RateLimited");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void ProviderUnreachable_WithReason_ReturnsUnavailableError()
    {
        // Arrange
        const string reason = "TCP connection refused";

        // Act
        var error = JobsErrors.ProviderUnreachable(reason);

        // Assert
        error.Code.Should().Be("Jobs.ProviderUnreachable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void AllErrorFactories_ReturnNonNullErrorInstances()
    {
        // Act / Assert
        JobsErrors.JobNotFound("j").Should().NotBeNull();
        JobsErrors.InvalidPayload("r").Should().NotBeNull();
        JobsErrors.CronInvalid("c").Should().NotBeNull();
        JobsErrors.RateLimited("r").Should().NotBeNull();
        JobsErrors.ProviderUnreachable("r").Should().NotBeNull();
    }

    [Fact]
    public void AllErrorCodes_StartWithJobsPrefix()
    {
        // Act
        var codes = new[]
        {
            JobsErrors.JobNotFound("j").Code,
            JobsErrors.InvalidPayload("r").Code,
            JobsErrors.CronInvalid("c").Code,
            JobsErrors.RateLimited("r").Code,
            JobsErrors.ProviderUnreachable("r").Code,
        };

        // Assert
        codes.Should().OnlyContain(c => c.StartsWith("Jobs.", StringComparison.Ordinal));
    }
}
