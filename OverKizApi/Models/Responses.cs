// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace OverKizApi.Models;

// Transport models remain internal; the client exposes domain models and validated values.
// Nullable fields let each endpoint report missing data using its own exception contract.

/// <summary>Result of standard, JWT or SSO login.</summary>
internal sealed class LoginResponse
	{
	[JsonPropertyName ("success")]
	public bool? Success { get; init; }
	}

/// <summary>Somfy OAuth token exchange, refresh or credential error.</summary>
internal sealed class SomfyTokenResponse
	{
	[JsonPropertyName ("access_token")]
	public string? AccessToken { get; init; }

	[JsonPropertyName ("refresh_token")]
	public string? RefreshToken { get; init; }

	[JsonPropertyName ("expires_in")]
	public int? ExpiresIn { get; init; }

	[JsonPropertyName ("message")]
	public string? Message { get; init; }
	}

/// <summary>Atlantic OAuth token exchange or credential error.</summary>
internal sealed class CozyTouchTokenResponse
	{
	[JsonPropertyName ("access_token")]
	public string? AccessToken { get; init; }

	[JsonPropertyName ("token_type")]
	public string? TokenType { get; init; }

	[JsonPropertyName ("error")]
	public string? Error { get; init; }

	[JsonPropertyName ("error_description")]
	public string? ErrorDescription { get; init; }
	}

/// <summary>Identifier assigned by event listener registration.</summary>
internal sealed class EventListenerResponse
	{
	[JsonPropertyName ("id")]
	public string? Id { get; init; }
	}

/// <summary>Identifier assigned to a device action or scenario execution.</summary>
internal sealed class ExecutionResponse
	{
	[JsonPropertyName ("execId")]
	public string? ExecId { get; init; }
	}

/// <summary>Identifier assigned to a scheduled scenario.</summary>
internal sealed class ScheduledExecutionResponse
	{
	[JsonPropertyName ("triggerId")]
	public string? TriggerId { get; init; }
	}

/// <summary>Local API token generated for a gateway.</summary>
internal sealed class LocalTokenGenerationResponse
	{
	[JsonPropertyName ("token")]
	public string? Token { get; init; }
	}

/// <summary>Identifier assigned to a local-token activation request.</summary>
internal sealed class LocalTokenActivationResponse
	{
	[JsonPropertyName ("requestId")]
	public string? RequestId { get; init; }
	}

/// <summary>Overkiz error envelope used by the exception mapping layer.</summary>
internal sealed class ApiErrorResponse
	{
	[JsonPropertyName ("error")]
	public string? Error { get; init; }
	}

/// <summary>Alternative wrappers used by gateways when returning device states.</summary>
internal sealed class DeviceStatesResponse
	{
	[JsonPropertyName ("states")]
	public IReadOnlyList<State>? States { get; init; }

	[JsonPropertyName ("deviceStates")]
	public IReadOnlyList<State>? DeviceStates { get; init; }

	[JsonPropertyName ("values")]
	public IReadOnlyList<State>? Values { get; init; }
	}