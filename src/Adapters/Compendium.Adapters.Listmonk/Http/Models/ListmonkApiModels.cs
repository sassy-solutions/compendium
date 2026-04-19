// -----------------------------------------------------------------------
// <copyright file="ListmonkApiModels.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Listmonk.Http.Models;

// ============================================================================
// Subscriber Models
// ============================================================================

/// <summary>
/// Represents a subscriber in Listmonk.
/// </summary>
internal sealed record ListmonkSubscriber
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("lists")]
    public List<ListmonkList>? Lists { get; init; }

    [JsonPropertyName("attribs")]
    public Dictionary<string, JsonElement>? Attributes { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Request to create a new subscriber.
/// </summary>
internal sealed record ListmonkCreateSubscriberRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "enabled";

    [JsonPropertyName("lists")]
    public List<int>? Lists { get; init; }

    [JsonPropertyName("attribs")]
    public Dictionary<string, object>? Attributes { get; init; }

    [JsonPropertyName("preconfirm_subscriptions")]
    public bool PreconfirmSubscriptions { get; init; }
}

/// <summary>
/// Request to update a subscriber.
/// </summary>
internal sealed record ListmonkUpdateSubscriberRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("lists")]
    public List<int>? Lists { get; init; }

    [JsonPropertyName("attribs")]
    public Dictionary<string, object>? Attributes { get; init; }
}

// ============================================================================
// List Models
// ============================================================================

/// <summary>
/// Represents a mailing list in Listmonk.
/// </summary>
internal sealed record ListmonkList
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("optin")]
    public string? Optin { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("subscriber_count")]
    public int SubscriberCount { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Request to create a new mailing list.
/// </summary>
internal sealed record ListmonkCreateListRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "private";

    [JsonPropertyName("optin")]
    public string Optin { get; init; } = "single";

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

// ============================================================================
// Template Models
// ============================================================================

/// <summary>
/// Represents a template in Listmonk.
/// </summary>
internal sealed record ListmonkTemplate
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("is_default")]
    public bool IsDefault { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

// ============================================================================
// Transactional Email Models
// ============================================================================

/// <summary>
/// Request to send a transactional email.
/// </summary>
internal sealed record ListmonkTransactionalRequest
{
    [JsonPropertyName("subscriber_email")]
    public string? SubscriberEmail { get; init; }

    [JsonPropertyName("subscriber_id")]
    public int? SubscriberId { get; init; }

    [JsonPropertyName("template_id")]
    public int TemplateId { get; init; }

    [JsonPropertyName("from_email")]
    public string? FromEmail { get; init; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; init; }

    [JsonPropertyName("headers")]
    public List<ListmonkHeader>? Headers { get; init; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = "html";

    [JsonPropertyName("messenger")]
    public string Messenger { get; init; } = "email";
}

/// <summary>
/// Represents an email header.
/// </summary>
internal sealed record ListmonkHeader
{
    [JsonPropertyName("header")]
    public string? Header { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

// ============================================================================
// Campaign Models
// ============================================================================

/// <summary>
/// Represents a campaign in Listmonk.
/// </summary>
internal sealed record ListmonkCampaign
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("from_email")]
    public string? FromEmail { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("altbody")]
    public string? AltBody { get; init; }

    [JsonPropertyName("send_at")]
    public DateTimeOffset? SendAt { get; init; }

    [JsonPropertyName("started_at")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("to_send")]
    public int ToSend { get; init; }

    [JsonPropertyName("sent")]
    public int Sent { get; init; }

    [JsonPropertyName("lists")]
    public List<ListmonkList>? Lists { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("template_id")]
    public int? TemplateId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Request to create a new campaign.
/// </summary>
internal sealed record ListmonkCreateCampaignRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("from_email")]
    public string? FromEmail { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "regular";

    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = "richtext";

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("altbody")]
    public string? AltBody { get; init; }

    [JsonPropertyName("send_at")]
    public DateTimeOffset? SendAt { get; init; }

    [JsonPropertyName("lists")]
    public List<int>? Lists { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("template_id")]
    public int? TemplateId { get; init; }
}

// ============================================================================
// Response Wrappers
// ============================================================================

/// <summary>
/// Generic API response wrapper.
/// </summary>
internal sealed record ListmonkResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

/// <summary>
/// Paginated results wrapper.
/// </summary>
internal sealed record ListmonkPaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public ListmonkPaginatedData<T>? Data { get; init; }
}

/// <summary>
/// Paginated data container.
/// </summary>
internal sealed record ListmonkPaginatedData<T>
{
    [JsonPropertyName("results")]
    public List<T>? Results { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }
}

/// <summary>
/// Error response from Listmonk API.
/// </summary>
internal sealed record ListmonkErrorResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
