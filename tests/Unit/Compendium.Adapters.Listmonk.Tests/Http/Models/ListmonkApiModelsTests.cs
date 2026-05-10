// -----------------------------------------------------------------------
// <copyright file="ListmonkApiModelsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using Compendium.Adapters.Listmonk.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.Listmonk.Tests.Http.Models;

/// <summary>
/// Round-trip tests for the internal Listmonk API model records. They are reached
/// indirectly through reflection (every record is <c>internal sealed</c>) and exercise
/// constructors, default values, and JSON property mappings. The HTTP client tests
/// cover serialization through the wire; these tests pin individual record shapes.
/// </summary>
public class ListmonkApiModelsTests
{
    private static readonly Assembly AdapterAssembly = typeof(ListmonkOptions).Assembly;

    private static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Theory]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkSubscriber")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkList")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkCampaign")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkTemplate")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkHeader")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateSubscriberRequest")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkUpdateSubscriberRequest")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateListRequest")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateCampaignRequest")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkTransactionalRequest")]
    [InlineData("Compendium.Adapters.Listmonk.Http.Models.ListmonkErrorResponse")]
    public void Models_AreInternalSealedRecords(string typeName)
    {
        // Arrange
        var type = AdapterAssembly.GetType(typeName, throwOnError: true)!;

        // Act / Assert
        type.IsSealed.Should().BeTrue("API DTOs are sealed records");
        type.IsNotPublic.Should().BeTrue("API DTOs are internal");
    }

    [Fact]
    public void ListmonkCampaign_DeserializesAllPropertiesFromJson()
    {
        // Arrange
        const string json =
            "{\"id\":1,\"uuid\":\"u\",\"name\":\"n\",\"subject\":\"s\",\"from_email\":\"f@x.y\"," +
            "\"status\":\"draft\",\"type\":\"regular\",\"body\":\"b\",\"altbody\":\"a\"," +
            "\"send_at\":\"2026-01-01T00:00:00Z\",\"started_at\":\"2026-01-02T00:00:00Z\"," +
            "\"to_send\":99,\"sent\":50,\"lists\":[],\"tags\":[\"t1\"],\"template_id\":7," +
            "\"created_at\":\"2026-01-01T00:00:00Z\",\"updated_at\":\"2026-01-02T00:00:00Z\"}";
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCampaign", throwOnError: true)!;

        // Act
        var campaign = JsonSerializer.Deserialize(json, type, SnakeCase)!;

        // Assert
        ReadProp<int>(campaign, "Id").Should().Be(1);
        ReadProp<string?>(campaign, "Uuid").Should().Be("u");
        ReadProp<string?>(campaign, "Name").Should().Be("n");
        ReadProp<string?>(campaign, "Subject").Should().Be("s");
        ReadProp<string?>(campaign, "FromEmail").Should().Be("f@x.y");
        ReadProp<string?>(campaign, "Status").Should().Be("draft");
        ReadProp<string?>(campaign, "Type").Should().Be("regular");
        ReadProp<string?>(campaign, "Body").Should().Be("b");
        ReadProp<string?>(campaign, "AltBody").Should().Be("a");
        ReadProp<int>(campaign, "ToSend").Should().Be(99);
        ReadProp<int>(campaign, "Sent").Should().Be(50);
        ReadProp<int?>(campaign, "TemplateId").Should().Be(7);
        ReadProp<DateTimeOffset?>(campaign, "SendAt").Should().NotBeNull();
        ReadProp<DateTimeOffset?>(campaign, "StartedAt").Should().NotBeNull();
        ReadProp<DateTimeOffset?>(campaign, "CreatedAt").Should().NotBeNull();
        ReadProp<DateTimeOffset?>(campaign, "UpdatedAt").Should().NotBeNull();
    }

    [Fact]
    public void ListmonkHeader_DeserializesNameAndValue()
    {
        // Arrange
        const string json = "{\"header\":\"X-Custom\",\"value\":\"yes\"}";
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkHeader", throwOnError: true)!;

        // Act
        var header = JsonSerializer.Deserialize(json, type, SnakeCase)!;

        // Assert
        ReadProp<string?>(header, "Header").Should().Be("X-Custom");
        ReadProp<string?>(header, "Value").Should().Be("yes");
    }

    [Fact]
    public void ListmonkTemplate_DeserializesAllProperties()
    {
        // Arrange
        const string json =
            "{\"id\":3,\"name\":\"Welcome\",\"type\":\"campaign\",\"body\":\"<p>hi</p>\"," +
            "\"is_default\":true,\"created_at\":\"2026-01-01T00:00:00Z\",\"updated_at\":\"2026-01-02T00:00:00Z\"}";
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkTemplate", throwOnError: true)!;

        // Act
        var template = JsonSerializer.Deserialize(json, type, SnakeCase)!;

        // Assert
        ReadProp<int>(template, "Id").Should().Be(3);
        ReadProp<string?>(template, "Name").Should().Be("Welcome");
        ReadProp<string?>(template, "Type").Should().Be("campaign");
        ReadProp<string?>(template, "Body").Should().Be("<p>hi</p>");
        ReadProp<bool>(template, "IsDefault").Should().BeTrue();
        ReadProp<DateTimeOffset?>(template, "CreatedAt").Should().NotBeNull();
        ReadProp<DateTimeOffset?>(template, "UpdatedAt").Should().NotBeNull();
    }

    [Fact]
    public void ListmonkCreateCampaignRequest_DefaultsAreCorrect()
    {
        // Arrange
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateCampaignRequest", throwOnError: true)!;

        // Act
        var instance = Activator.CreateInstance(type)!;

        // Assert
        ReadProp<string?>(instance, "Type").Should().Be("regular");
        ReadProp<string?>(instance, "ContentType").Should().Be("richtext");
        ReadProp<string?>(instance, "Name").Should().BeNull();
        ReadProp<string?>(instance, "Subject").Should().BeNull();
        ReadProp<string?>(instance, "FromEmail").Should().BeNull();
        ReadProp<string?>(instance, "Body").Should().BeNull();
        ReadProp<string?>(instance, "AltBody").Should().BeNull();
        ReadProp<int?>(instance, "TemplateId").Should().BeNull();
    }

    [Fact]
    public void ListmonkCreateListRequest_DefaultsAreCorrect()
    {
        // Arrange
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateListRequest", throwOnError: true)!;

        // Act
        var instance = Activator.CreateInstance(type)!;

        // Assert
        ReadProp<string?>(instance, "Type").Should().Be("private");
        ReadProp<string?>(instance, "Optin").Should().Be("single");
        ReadProp<string?>(instance, "Name").Should().BeNull();
        ReadProp<string?>(instance, "Description").Should().BeNull();
    }

    [Fact]
    public void ListmonkTransactionalRequest_DefaultsAreCorrect()
    {
        // Arrange
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkTransactionalRequest", throwOnError: true)!;

        // Act
        var instance = Activator.CreateInstance(type)!;

        // Assert
        ReadProp<string?>(instance, "ContentType").Should().Be("html");
        ReadProp<string?>(instance, "Messenger").Should().Be("email");
        ReadProp<int>(instance, "TemplateId").Should().Be(0);
        ReadProp<string?>(instance, "SubscriberEmail").Should().BeNull();
        ReadProp<int?>(instance, "SubscriberId").Should().BeNull();
    }

    [Fact]
    public void ListmonkCreateSubscriberRequest_DefaultStatusIsEnabled()
    {
        // Arrange
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkCreateSubscriberRequest", throwOnError: true)!;

        // Act
        var instance = Activator.CreateInstance(type)!;

        // Assert
        ReadProp<string?>(instance, "Status").Should().Be("enabled");
        ReadProp<bool>(instance, "PreconfirmSubscriptions").Should().BeFalse();
    }

    [Fact]
    public void ListmonkErrorResponse_DeserializesMessage()
    {
        // Arrange
        const string json = "{\"message\":\"something broke\"}";
        var type = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkErrorResponse", throwOnError: true)!;

        // Act
        var error = JsonSerializer.Deserialize(json, type, SnakeCase)!;

        // Assert
        ReadProp<string?>(error, "Message").Should().Be("something broke");
    }

    [Fact]
    public void ListmonkPaginatedData_DefaultsArePresent()
    {
        // Arrange
        var dataType = AdapterAssembly.GetType(
            "Compendium.Adapters.Listmonk.Http.Models.ListmonkPaginatedData`1", throwOnError: true)!
            .MakeGenericType(AdapterAssembly.GetType(
                "Compendium.Adapters.Listmonk.Http.Models.ListmonkSubscriber", throwOnError: true)!);

        // Act
        var data = Activator.CreateInstance(dataType)!;

        // Assert
        ReadProp<int>(data, "Total").Should().Be(0);
        ReadProp<int>(data, "Page").Should().Be(0);
        ReadProp<int>(data, "PerPage").Should().Be(0);
    }

    private static T? ReadProp<T>(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException(
                $"Property '{propertyName}' not found on {instance.GetType().FullName}");
        var value = prop.GetValue(instance);
        return value is null ? default : (T)value;
    }
}
