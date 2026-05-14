// -----------------------------------------------------------------------
// <copyright file="ICrmSyncTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Tests;

public class ICrmSyncTests
{
    [Fact]
    public async Task UpsertContactAsync_CanBeSubstituted_AndReturnsConfiguredResult()
    {
        // Arrange
        var sync = Substitute.For<ICrmSync>();
        var contact = new CrmContact("ext-1", "user@example.com", "Ada", "L", null, "tenant-1");
        sync.UpsertContactAsync(contact, Arg.Any<CancellationToken>())
            .Returns(Result.Success("provider-id-1"));

        // Act
        var result = await sync.UpsertContactAsync(contact, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("provider-id-1");
    }

    [Fact]
    public async Task UpsertCompanyAsync_CanBeSubstituted_AndReturnsConfiguredResult()
    {
        // Arrange
        var sync = Substitute.For<ICrmSync>();
        var company = new CrmCompany("co-1", "Acme", null, null, "tenant-1");
        sync.UpsertCompanyAsync(company, Arg.Any<CancellationToken>())
            .Returns(Result.Success("provider-co-1"));

        // Act
        var result = await sync.UpsertCompanyAsync(company, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("provider-co-1");
    }

    [Fact]
    public async Task TrackEventAsync_CanBeSubstituted_AndReturnsConfiguredResult()
    {
        // Arrange
        var sync = Substitute.For<ICrmSync>();
        var evt = new CrmEvent("trial_started", "ext-1", null, DateTimeOffset.UtcNow, "tenant-1");
        sync.TrackEventAsync(evt, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await sync.TrackEventAsync(evt, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AssociateAsync_CanBeSubstituted_AndPropagatesFailure()
    {
        // Arrange
        var sync = Substitute.For<ICrmSync>();
        var association = new CrmAssociation("contact", "ext-1", "company", "co-1", null);
        var error = CrmErrors.ProviderUnreachable("hubspot");
        sync.AssociateAsync(association, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        var result = await sync.AssociateAsync(association, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Crm.ProviderUnreachable");
    }
}
