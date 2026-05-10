// -----------------------------------------------------------------------
// <copyright file="EmailSendResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests.Models;

public class EmailSendResultTests
{
    [Fact]
    public void EmailSendResult_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange / Act
        var result = new EmailSendResult
        {
            MessageId = "msg-123",
            Status = EmailStatus.Queued,
        };

        // Assert
        result.MessageId.Should().Be("msg-123");
        result.Status.Should().Be(EmailStatus.Queued);
        result.SentAt.Should().Be(default);
        result.ProviderData.Should().BeNull();
    }

    [Fact]
    public void EmailSendResult_WithAllProperties_PreservesValues()
    {
        // Arrange
        var sentAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var providerData = new Dictionary<string, object>
        {
            ["provider"] = "listmonk",
            ["responseCode"] = 202,
        };

        // Act
        var result = new EmailSendResult
        {
            MessageId = "msg-7",
            Status = EmailStatus.Sent,
            SentAt = sentAt,
            ProviderData = providerData,
        };

        // Assert
        result.MessageId.Should().Be("msg-7");
        result.Status.Should().Be(EmailStatus.Sent);
        result.SentAt.Should().Be(sentAt);
        result.ProviderData.Should().BeSameAs(providerData);
    }

    [Theory]
    [InlineData(EmailStatus.Queued)]
    [InlineData(EmailStatus.Sending)]
    [InlineData(EmailStatus.Sent)]
    [InlineData(EmailStatus.Delivered)]
    [InlineData(EmailStatus.Opened)]
    [InlineData(EmailStatus.Clicked)]
    [InlineData(EmailStatus.Bounced)]
    [InlineData(EmailStatus.SpamComplaint)]
    [InlineData(EmailStatus.Failed)]
    public void EmailSendResult_AllStatuses_AreAcceptedAsInitValues(EmailStatus status)
    {
        // Arrange / Act
        var result = new EmailSendResult
        {
            MessageId = "id",
            Status = status,
        };

        // Assert
        result.Status.Should().Be(status);
    }

    [Fact]
    public void EmailSendResult_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new EmailSendResult
        {
            MessageId = "msg-1",
            Status = EmailStatus.Queued,
        };

        // Act
        var updated = original with { Status = EmailStatus.Sent };

        // Assert
        updated.Status.Should().Be(EmailStatus.Sent);
        original.Status.Should().Be(EmailStatus.Queued);
        updated.MessageId.Should().Be("msg-1");
    }
}

public class BatchEmailResultTests
{
    [Fact]
    public void BatchEmailResult_FailedCount_DerivesFromTotalAndSuccess()
    {
        // Arrange
        var items = new[]
        {
            new BatchEmailItemResult { To = "a@b.co", Success = true, MessageId = "m-1" },
            new BatchEmailItemResult { To = "c@d.co", Success = false, ErrorMessage = "bounced" },
            new BatchEmailItemResult { To = "e@f.co", Success = false, ErrorMessage = "blocked" },
        };

        // Act
        var batch = new BatchEmailResult
        {
            TotalCount = 3,
            SuccessCount = 1,
            Results = items,
        };

        // Assert
        batch.TotalCount.Should().Be(3);
        batch.SuccessCount.Should().Be(1);
        batch.FailedCount.Should().Be(2);
        batch.Results.Should().HaveCount(3);
    }

    [Fact]
    public void BatchEmailResult_AllSuccess_HasZeroFailed()
    {
        // Arrange / Act
        var batch = new BatchEmailResult
        {
            TotalCount = 5,
            SuccessCount = 5,
            Results = Array.Empty<BatchEmailItemResult>(),
        };

        // Assert
        batch.FailedCount.Should().Be(0);
    }

    [Fact]
    public void BatchEmailResult_AllFailed_FailedCountEqualsTotal()
    {
        // Arrange / Act
        var batch = new BatchEmailResult
        {
            TotalCount = 4,
            SuccessCount = 0,
            Results = Array.Empty<BatchEmailItemResult>(),
        };

        // Assert
        batch.FailedCount.Should().Be(4);
    }

    [Fact]
    public void BatchEmailResult_EmptyBatch_HasZeroFailed()
    {
        // Arrange / Act
        var batch = new BatchEmailResult
        {
            TotalCount = 0,
            SuccessCount = 0,
            Results = Array.Empty<BatchEmailItemResult>(),
        };

        // Assert
        batch.FailedCount.Should().Be(0);
        batch.Results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(10, 7, 3)]
    [InlineData(100, 100, 0)]
    [InlineData(50, 0, 50)]
    [InlineData(1, 0, 1)]
    public void BatchEmailResult_FailedCount_IsTotalMinusSuccess(int total, int success, int expectedFailed)
    {
        // Arrange / Act
        var batch = new BatchEmailResult
        {
            TotalCount = total,
            SuccessCount = success,
            Results = Array.Empty<BatchEmailItemResult>(),
        };

        // Assert
        batch.FailedCount.Should().Be(expectedFailed);
    }
}

public class BatchEmailItemResultTests
{
    [Fact]
    public void BatchEmailItemResult_Success_HasMessageIdAndNoError()
    {
        // Arrange / Act
        var item = new BatchEmailItemResult
        {
            To = "alice@example.com",
            Success = true,
            MessageId = "m-42",
        };

        // Assert
        item.To.Should().Be("alice@example.com");
        item.Success.Should().BeTrue();
        item.MessageId.Should().Be("m-42");
        item.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void BatchEmailItemResult_Failure_HasErrorMessageAndNoMessageId()
    {
        // Arrange / Act
        var item = new BatchEmailItemResult
        {
            To = "bounce@example.com",
            Success = false,
            ErrorMessage = "Hard bounce",
        };

        // Assert
        item.Success.Should().BeFalse();
        item.MessageId.Should().BeNull();
        item.ErrorMessage.Should().Be("Hard bounce");
    }

    [Fact]
    public void BatchEmailItemResult_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new BatchEmailItemResult { To = "a@b.co", Success = true, MessageId = "m-1" };

        // Act
        var updated = original with { Success = false, ErrorMessage = "rate limited" };

        // Assert
        updated.Success.Should().BeFalse();
        updated.ErrorMessage.Should().Be("rate limited");
        updated.To.Should().Be(original.To);
    }
}

public class EmailStatusTests
{
    [Fact]
    public void EmailStatus_HasExpectedNumericValues()
    {
        // Assert
        ((int)EmailStatus.Queued).Should().Be(0);
        ((int)EmailStatus.Sending).Should().Be(1);
        ((int)EmailStatus.Sent).Should().Be(2);
        ((int)EmailStatus.Delivered).Should().Be(3);
        ((int)EmailStatus.Opened).Should().Be(4);
        ((int)EmailStatus.Clicked).Should().Be(5);
        ((int)EmailStatus.Bounced).Should().Be(6);
        ((int)EmailStatus.SpamComplaint).Should().Be(7);
        ((int)EmailStatus.Failed).Should().Be(8);
    }

    [Fact]
    public void EmailStatus_DefinesAllNineStates()
    {
        // Act
        var values = Enum.GetValues<EmailStatus>();

        // Assert
        values.Should().HaveCount(9);
    }
}
