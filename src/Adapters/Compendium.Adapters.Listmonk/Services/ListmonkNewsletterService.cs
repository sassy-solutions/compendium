// -----------------------------------------------------------------------
// <copyright file="ListmonkNewsletterService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.Http;
using Compendium.Adapters.Listmonk.Http.Models;

namespace Compendium.Adapters.Listmonk.Services;

/// <summary>
/// Implements newsletter service using Listmonk REST API.
/// </summary>
internal sealed class ListmonkNewsletterService : INewsletterService
{
    private readonly ListmonkHttpClient _httpClient;
    private readonly ListmonkOptions _options;
    private readonly ILogger<ListmonkNewsletterService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListmonkNewsletterService"/> class.
    /// </summary>
    public ListmonkNewsletterService(
        ListmonkHttpClient httpClient,
        IOptions<ListmonkOptions> options,
        ILogger<ListmonkNewsletterService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<Subscriber>> SubscribeAsync(
        SubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Subscribing to newsletter (activity {ActivityId})", Activity.Current?.Id);

        var listIds = new List<int>();

        if (request.ListIds is not null)
        {
            foreach (var listId in request.ListIds)
            {
                if (int.TryParse(listId, out var id))
                {
                    listIds.Add(id);
                }
            }
        }

        if (listIds.Count == 0 && _options.DefaultListId.HasValue)
        {
            listIds.Add(_options.DefaultListId.Value);
        }

        var listmonkRequest = new ListmonkCreateSubscriberRequest
        {
            Email = request.Email,
            Name = request.Name,
            Status = "enabled",
            Lists = listIds,
            Attributes = request.Attributes?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            PreconfirmSubscriptions = !request.RequireConfirmation
        };

        var result = await _httpClient.CreateSubscriberAsync(listmonkRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to subscribe (activity {ActivityId}): {Error}", Activity.Current?.Id, result.Error.Message);
            return result.Error;
        }

        var subscriber = MapToSubscriber(result.Value);
        _logger.LogInformation("Subscribed with ID {SubscriberId}", subscriber.Id);

        return subscriber;
    }

    /// <inheritdoc />
    public async Task<Result> UnsubscribeAsync(
        string email,
        string? listId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        _logger.LogInformation("Unsubscribing from list {ListId} (activity {ActivityId})", listId ?? "all", Activity.Current?.Id);

        // First, find the subscriber by email
        var subscriberResult = await _httpClient.GetSubscriberByEmailAsync(email, cancellationToken);
        if (subscriberResult.IsFailure)
        {
            return subscriberResult.Error;
        }

        var subscriberId = subscriberResult.Value.Id;

        if (listId is not null && int.TryParse(listId, out var listIdInt))
        {
            // Unsubscribe from specific list
            var result = await _httpClient.RemoveSubscriberFromListsAsync(
                subscriberId, new List<int> { listIdInt }, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to unsubscribe {SubscriberId} from list {ListId}: {Error}",
                    subscriberId, listId, result.Error.Message);
            }
            else
            {
                _logger.LogInformation("Unsubscribed {SubscriberId} from list {ListId}", subscriberId, listId);
            }

            return result;
        }
        else
        {
            // Unsubscribe from all lists by disabling the subscriber
            var updateRequest = new ListmonkUpdateSubscriberRequest
            {
                Status = "disabled"
            };

            var result = await _httpClient.UpdateSubscriberAsync(subscriberId, updateRequest, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to unsubscribe {SubscriberId}: {Error}", subscriberId, result.Error.Message);
            }
            else
            {
                _logger.LogInformation("Unsubscribed {SubscriberId} from all lists", subscriberId);
            }

            return result.IsSuccess ? Result.Success() : result.Error;
        }
    }

    /// <inheritdoc />
    public async Task<Result<Subscriber>> GetSubscriberAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        _logger.LogDebug("Getting subscriber by email (activity {ActivityId})", Activity.Current?.Id);

        var result = await _httpClient.GetSubscriberByEmailAsync(email, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        return MapToSubscriber(result.Value);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateSubscriberAttributesAsync(
        string email,
        IReadOnlyDictionary<string, object> attributes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(attributes);

        _logger.LogInformation("Updating subscriber attributes (activity {ActivityId})", Activity.Current?.Id);

        // First, find the subscriber by email
        var subscriberResult = await _httpClient.GetSubscriberByEmailAsync(email, cancellationToken);
        if (subscriberResult.IsFailure)
        {
            return subscriberResult.Error;
        }

        var updateRequest = new ListmonkUpdateSubscriberRequest
        {
            Attributes = attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        var result = await _httpClient.UpdateSubscriberAsync(
            subscriberResult.Value.Id, updateRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to update attributes for {SubscriberId}: {Error}",
                subscriberResult.Value.Id, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Updated attributes for subscriber {SubscriberId}", subscriberResult.Value.Id);
        }

        return result.IsSuccess ? Result.Success() : result.Error;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MailingList>>> ListMailingListsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing mailing lists");

        var result = await _httpClient.ListListsAsync(1, 100, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var lists = result.Value.Results?
            .Select(MapToMailingList)
            .ToList() ?? new List<MailingList>();

        return lists;
    }

    /// <inheritdoc />
    public Task<Result> ConfirmSubscriptionAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        // Listmonk handles confirmation through its own web interface
        // This endpoint would need to be called by the subscriber clicking the confirmation link
        _logger.LogWarning("Subscription confirmation is handled directly by Listmonk via confirmation links");

        return Task.FromResult(Result.Success());
    }

    // ============================================================================
    // Mapping Helpers
    // ============================================================================

    private static Subscriber MapToSubscriber(ListmonkSubscriber listmonkSubscriber)
    {
        return new Subscriber
        {
            Id = listmonkSubscriber.Id.ToString(),
            Email = listmonkSubscriber.Email ?? string.Empty,
            Name = listmonkSubscriber.Name,
            Status = MapSubscriberStatus(listmonkSubscriber.Status),
            ListIds = listmonkSubscriber.Lists?.Select(l => l.Id.ToString()).ToList().AsReadOnly(),
            Attributes = listmonkSubscriber.Attributes?
                .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.ToString()!),
            CreatedAt = listmonkSubscriber.CreatedAt ?? DateTimeOffset.MinValue,
            UpdatedAt = listmonkSubscriber.UpdatedAt,
            ConfirmedAt = listmonkSubscriber.Status == "enabled" ? listmonkSubscriber.CreatedAt : null
        };
    }

    private static SubscriptionStatus MapSubscriberStatus(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "enabled" => SubscriptionStatus.Confirmed,
            "disabled" => SubscriptionStatus.Unsubscribed,
            "blocklisted" => SubscriptionStatus.Blocked,
            _ => SubscriptionStatus.Pending
        };
    }

    private static MailingList MapToMailingList(ListmonkList listmonkList)
    {
        return new MailingList
        {
            Id = listmonkList.Id.ToString(),
            Slug = listmonkList.Uuid ?? listmonkList.Id.ToString(),
            Name = listmonkList.Name ?? string.Empty,
            Description = listmonkList.Description,
            IsPublic = listmonkList.Type == "public",
            IsSingleOptIn = listmonkList.Optin == "single",
            SubscriberCount = listmonkList.SubscriberCount,
            CreatedAt = listmonkList.CreatedAt ?? DateTimeOffset.MinValue,
            UpdatedAt = listmonkList.UpdatedAt
        };
    }
}
