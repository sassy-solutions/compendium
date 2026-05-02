// -----------------------------------------------------------------------
// <copyright file="ZitadelApiModels.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Zitadel.Http.Models;

/// <summary>
/// Represents a Zitadel human user response.
/// </summary>
internal sealed record ZitadelUser
{
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("userName")]
    public string? UserName { get; init; }

    [JsonPropertyName("loginNames")]
    public List<string>? LoginNames { get; init; }

    [JsonPropertyName("preferredLoginName")]
    public string? PreferredLoginName { get; init; }

    [JsonPropertyName("human")]
    public ZitadelHumanDetails? Human { get; init; }

    [JsonPropertyName("details")]
    public ZitadelResourceDetails? Details { get; init; }
}

/// <summary>
/// Represents human-specific details for a Zitadel user.
/// </summary>
internal sealed record ZitadelHumanDetails
{
    [JsonPropertyName("profile")]
    public ZitadelProfile? Profile { get; init; }

    [JsonPropertyName("email")]
    public ZitadelEmail? Email { get; init; }

    [JsonPropertyName("phone")]
    public ZitadelPhone? Phone { get; init; }

    [JsonPropertyName("passwordChangeRequired")]
    public bool? PasswordChangeRequired { get; init; }
}

/// <summary>
/// Represents a Zitadel user profile.
/// </summary>
internal sealed record ZitadelProfile
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("nickName")]
    public string? NickName { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Represents email information in Zitadel.
/// </summary>
internal sealed record ZitadelEmail
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("isEmailVerified")]
    public bool? IsEmailVerified { get; init; }
}

/// <summary>
/// Represents phone information in Zitadel.
/// </summary>
internal sealed record ZitadelPhone
{
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("isPhoneVerified")]
    public bool? IsPhoneVerified { get; init; }
}

/// <summary>
/// Represents resource metadata in Zitadel responses.
/// </summary>
internal sealed record ZitadelResourceDetails
{
    [JsonPropertyName("sequence")]
    public long? Sequence { get; init; }

    [JsonPropertyName("creationDate")]
    public DateTimeOffset? CreationDate { get; init; }

    [JsonPropertyName("changeDate")]
    public DateTimeOffset? ChangeDate { get; init; }

    [JsonPropertyName("resourceOwner")]
    public string? ResourceOwner { get; init; }
}

/// <summary>
/// Represents a request to create a human user in Zitadel.
/// </summary>
internal sealed record ZitadelCreateUserRequest
{
    [JsonPropertyName("userName")]
    public required string UserName { get; init; }

    [JsonPropertyName("profile")]
    public required ZitadelCreateProfile Profile { get; init; }

    [JsonPropertyName("email")]
    public required ZitadelCreateEmail Email { get; init; }

    [JsonPropertyName("phone")]
    public ZitadelCreatePhone? Phone { get; init; }

    [JsonPropertyName("password")]
    public ZitadelCreatePassword? Password { get; init; }

    [JsonPropertyName("metadata")]
    public List<ZitadelMetadataItem>? Metadata { get; init; }
}

/// <summary>
/// Represents profile data for creating a user.
/// </summary>
internal sealed record ZitadelCreateProfile
{
    [JsonPropertyName("givenName")]
    public required string FirstName { get; init; }

    [JsonPropertyName("familyName")]
    public required string LastName { get; init; }

    [JsonPropertyName("nickName")]
    public string? NickName { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }
}

/// <summary>
/// Represents email data for creating a user.
/// </summary>
internal sealed record ZitadelCreateEmail
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("isEmailVerified")]
    public bool? IsEmailVerified { get; init; }
}

/// <summary>
/// Represents phone data for creating a user.
/// </summary>
internal sealed record ZitadelCreatePhone
{
    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    [JsonPropertyName("isPhoneVerified")]
    public bool? IsPhoneVerified { get; init; }
}

/// <summary>
/// Represents password data for creating a user.
/// </summary>
internal sealed record ZitadelCreatePassword
{
    [JsonPropertyName("password")]
    public required string Password { get; init; }

    [JsonPropertyName("changeRequired")]
    public bool? ChangeRequired { get; init; }
}

/// <summary>
/// Represents a metadata item in Zitadel.
/// </summary>
internal sealed record ZitadelMetadataItem
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

/// <summary>
/// Represents a user search request.
/// </summary>
internal sealed record ZitadelUserSearchRequest
{
    [JsonPropertyName("query")]
    public ZitadelSearchQuery? Query { get; init; }

    [JsonPropertyName("queries")]
    public List<ZitadelUserQuery>? Queries { get; init; }
}

/// <summary>
/// Represents search query parameters.
/// </summary>
internal sealed record ZitadelSearchQuery
{
    [JsonPropertyName("offset")]
    public long? Offset { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("asc")]
    public bool? Asc { get; init; }
}

/// <summary>
/// Represents a user query filter.
/// </summary>
internal sealed record ZitadelUserQuery
{
    [JsonPropertyName("emailQuery")]
    public ZitadelEmailQuery? EmailQuery { get; init; }

    [JsonPropertyName("userNameQuery")]
    public ZitadelUserNameQuery? UserNameQuery { get; init; }

    [JsonPropertyName("displayNameQuery")]
    public ZitadelDisplayNameQuery? DisplayNameQuery { get; init; }
}

/// <summary>
/// Represents an email query filter.
/// </summary>
internal sealed record ZitadelEmailQuery
{
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }
}

/// <summary>
/// Represents a username query filter.
/// </summary>
internal sealed record ZitadelUserNameQuery
{
    [JsonPropertyName("userName")]
    public required string UserName { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }
}

/// <summary>
/// Represents a display name query filter.
/// </summary>
internal sealed record ZitadelDisplayNameQuery
{
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }
}

/// <summary>
/// Represents a user search response.
/// </summary>
internal sealed record ZitadelUserSearchResponse
{
    [JsonPropertyName("details")]
    public ZitadelListDetails? Details { get; init; }

    [JsonPropertyName("result")]
    public List<ZitadelUser>? Result { get; init; }
}

/// <summary>
/// Represents list metadata in Zitadel responses.
/// </summary>
internal sealed record ZitadelListDetails
{
    [JsonPropertyName("totalResult")]
    public long? TotalResult { get; init; }

    [JsonPropertyName("processedSequence")]
    public long? ProcessedSequence { get; init; }

    [JsonPropertyName("viewTimestamp")]
    public DateTimeOffset? ViewTimestamp { get; init; }
}

/// <summary>
/// Represents a token introspection response.
/// </summary>
internal sealed record ZitadelIntrospectionResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("exp")]
    public long? Exp { get; init; }

    [JsonPropertyName("iat")]
    public long? Iat { get; init; }

    [JsonPropertyName("sub")]
    public string? Sub { get; init; }

    [JsonPropertyName("aud")]
    public object? Aud { get; init; }

    [JsonPropertyName("iss")]
    public string? Iss { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; init; }

    [JsonPropertyName("urn:zitadel:iam:org:id")]
    public string? OrgId { get; init; }

    [JsonPropertyName("urn:zitadel:iam:user:resourceowner:id")]
    public string? ResourceOwnerId { get; init; }

    [JsonPropertyName("urn:zitadel:iam:org:project:roles")]
    public Dictionary<string, Dictionary<string, string>>? Roles { get; init; }
}

/// <summary>
/// Represents a Zitadel organization.
/// </summary>
internal sealed record ZitadelOrganization
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("primaryDomain")]
    public string? PrimaryDomain { get; init; }

    [JsonPropertyName("details")]
    public ZitadelResourceDetails? Details { get; init; }
}

/// <summary>
/// Represents a request to create an organization.
/// </summary>
internal sealed record ZitadelCreateOrganizationRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("admins")]
    public List<ZitadelOrgAdmin>? Admins { get; init; }
}

/// <summary>
/// Represents an organization admin.
/// </summary>
internal sealed record ZitadelOrgAdmin
{
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }
}

/// <summary>
/// Represents an organization member.
/// </summary>
internal sealed record ZitadelOrgMember
{
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; init; }

    [JsonPropertyName("details")]
    public ZitadelResourceDetails? Details { get; init; }
}

/// <summary>
/// Represents a member list response.
/// </summary>
internal sealed record ZitadelMemberListResponse
{
    [JsonPropertyName("details")]
    public ZitadelListDetails? Details { get; init; }

    [JsonPropertyName("result")]
    public List<ZitadelOrgMember>? Result { get; init; }
}

/// <summary>
/// Represents an add member request.
/// </summary>
internal sealed record ZitadelAddMemberRequest
{
    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("roles")]
    public required List<string> Roles { get; init; }
}

/// <summary>
/// Represents an OAuth2 token response.
/// </summary>
internal sealed record ZitadelTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}

/// <summary>
/// Represents a Zitadel project.
/// </summary>
internal sealed record ZitadelProject
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("details")]
    public ZitadelResourceDetails? Details { get; init; }
}

/// <summary>
/// Represents a request to create a project.
/// </summary>
internal sealed record ZitadelCreateProjectRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("projectRoleAssertion")]
    public bool ProjectRoleAssertion { get; init; }

    [JsonPropertyName("projectRoleCheck")]
    public bool ProjectRoleCheck { get; init; }

    [JsonPropertyName("hasProjectCheck")]
    public bool HasProjectCheck { get; init; }
}

/// <summary>
/// Represents a request to create an OIDC application.
/// </summary>
internal sealed record ZitadelCreateOidcAppRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("redirectUris")]
    public required List<string> RedirectUris { get; init; }

    [JsonPropertyName("postLogoutRedirectUris")]
    public required List<string> PostLogoutRedirectUris { get; init; }

    [JsonPropertyName("responseTypes")]
    public required List<string> ResponseTypes { get; init; }

    [JsonPropertyName("grantTypes")]
    public required List<string> GrantTypes { get; init; }

    [JsonPropertyName("appType")]
    public required string AppType { get; init; }

    [JsonPropertyName("authMethodType")]
    public required string AuthMethodType { get; init; }

    [JsonPropertyName("accessTokenType")]
    public string? AccessTokenType { get; init; }

    [JsonPropertyName("accessTokenRoleAssertion")]
    public bool? AccessTokenRoleAssertion { get; init; }

    [JsonPropertyName("idTokenRoleAssertion")]
    public bool? IdTokenRoleAssertion { get; init; }

    [JsonPropertyName("idTokenUserinfoAssertion")]
    public bool? IdTokenUserinfoAssertion { get; init; }
}

/// <summary>
/// Represents the response from creating an OIDC application.
/// </summary>
internal sealed record ZitadelOidcApp
{
    [JsonPropertyName("appId")]
    public string? AppId { get; init; }

    [JsonPropertyName("details")]
    public ZitadelResourceDetails? Details { get; init; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; init; }

    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; init; }
}

/// <summary>
/// Represents a request to update an OIDC application's settings.
/// </summary>
internal sealed record ZitadelUpdateOidcAppRequest
{
    [JsonPropertyName("redirectUris")]
    public List<string>? RedirectUris { get; init; }

    [JsonPropertyName("postLogoutRedirectUris")]
    public List<string>? PostLogoutRedirectUris { get; init; }

    [JsonPropertyName("responseTypes")]
    public List<string>? ResponseTypes { get; init; }

    [JsonPropertyName("grantTypes")]
    public List<string>? GrantTypes { get; init; }

    [JsonPropertyName("appType")]
    public string? AppType { get; init; }

    [JsonPropertyName("authMethodType")]
    public string? AuthMethodType { get; init; }

    [JsonPropertyName("accessTokenType")]
    public string? AccessTokenType { get; init; }

    [JsonPropertyName("accessTokenRoleAssertion")]
    public bool? AccessTokenRoleAssertion { get; init; }

    [JsonPropertyName("idTokenRoleAssertion")]
    public bool? IdTokenRoleAssertion { get; init; }

    [JsonPropertyName("idTokenUserinfoAssertion")]
    public bool? IdTokenUserinfoAssertion { get; init; }
}

/// <summary>
/// Represents an error response from Zitadel.
/// </summary>
internal sealed record ZitadelErrorResponse
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("details")]
    public List<ZitadelErrorDetail>? Details { get; init; }
}

/// <summary>
/// Represents an error detail in Zitadel error responses.
/// </summary>
internal sealed record ZitadelErrorDetail
{
    [JsonPropertyName("@type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>
/// Search request for organizations (POST /v2/organizations/_search).
/// </summary>
internal sealed record ZitadelOrganizationSearchRequest
{
    [JsonPropertyName("query")]
    public ZitadelSearchQuery? Query { get; init; }

    [JsonPropertyName("queries")]
    public List<ZitadelOrganizationQuery>? Queries { get; init; }
}

/// <summary>
/// Single query filter for organization search.
/// </summary>
internal sealed record ZitadelOrganizationQuery
{
    [JsonPropertyName("nameQuery")]
    public ZitadelOrgNameQuery? NameQuery { get; init; }
}

/// <summary>
/// Name query filter for organization search.
/// </summary>
internal sealed record ZitadelOrgNameQuery
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }
}

/// <summary>
/// Search response wrapping a list of organizations.
/// </summary>
internal sealed record ZitadelOrganizationSearchResponse
{
    [JsonPropertyName("details")]
    public ZitadelListDetails? Details { get; init; }

    [JsonPropertyName("result")]
    public List<ZitadelOrganization>? Result { get; init; }
}

/// <summary>
/// Search request for projects (POST /management/v1/projects/_search).
/// </summary>
internal sealed record ZitadelProjectSearchRequest
{
    [JsonPropertyName("query")]
    public ZitadelSearchQuery? Query { get; init; }

    [JsonPropertyName("queries")]
    public List<ZitadelProjectQuery>? Queries { get; init; }
}

/// <summary>
/// Single query filter for project search.
/// </summary>
internal sealed record ZitadelProjectQuery
{
    [JsonPropertyName("nameQuery")]
    public ZitadelProjectNameQuery? NameQuery { get; init; }
}

/// <summary>
/// Name query filter for project search.
/// </summary>
internal sealed record ZitadelProjectNameQuery
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }
}

/// <summary>
/// Search response wrapping a list of projects.
/// </summary>
internal sealed record ZitadelProjectSearchResponse
{
    [JsonPropertyName("details")]
    public ZitadelListDetails? Details { get; init; }

    [JsonPropertyName("result")]
    public List<ZitadelProject>? Result { get; init; }
}

/// <summary>
/// Search request for OIDC applications within a project
/// (POST /management/v1/projects/{projectId}/apps/_search).
/// </summary>
internal sealed record ZitadelAppSearchRequest
{
    [JsonPropertyName("query")]
    public ZitadelSearchQuery? Query { get; init; }

    [JsonPropertyName("queries")]
    public List<ZitadelAppQuery>? Queries { get; init; }
}

/// <summary>
/// Single query filter for application search.
/// </summary>
internal sealed record ZitadelAppQuery
{
    [JsonPropertyName("nameQuery")]
    public ZitadelAppNameQuery? NameQuery { get; init; }
}

/// <summary>
/// Name query filter for application search.
/// </summary>
internal sealed record ZitadelAppNameQuery
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }
}

/// <summary>
/// Search response wrapping a list of applications.
/// </summary>
internal sealed record ZitadelAppSearchResponse
{
    [JsonPropertyName("details")]
    public ZitadelListDetails? Details { get; init; }

    [JsonPropertyName("result")]
    public List<ZitadelApp>? Result { get; init; }
}

/// <summary>
/// Application list item returned from app search
/// (subset — does not include client_secret, which Zitadel only returns once at creation).
/// </summary>
internal sealed record ZitadelApp
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("oidcConfig")]
    public ZitadelAppOidcConfigSummary? OidcConfig { get; init; }
}

/// <summary>
/// Subset of OIDC config returned by app search — notably without client_secret.
/// </summary>
internal sealed record ZitadelAppOidcConfigSummary
{
    [JsonPropertyName("clientId")]
    public string? ClientId { get; init; }
}
