// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace OverKizApi.Models;

internal sealed class EmptyRequest { }

internal sealed class LoginRequest
	{
	[JsonPropertyName ("userId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? UserId { get; init; }
	[JsonPropertyName ("userPassword")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? UserPassword { get; init; }
	[JsonPropertyName ("jwt")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Jwt { get; init; }
	[JsonPropertyName ("ssoToken")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? SsoToken { get; init; }
	}

internal sealed class DeviceActionRequest
	{
	[JsonPropertyName ("label")]
	public required string Label { get; init; }
	[JsonPropertyName ("actions")]
	public required IReadOnlyList<Action> Actions { get; init; }
	}

internal sealed class LocalTokenActivationRequest
	{
	[JsonPropertyName ("label")]
	public required string Label { get; init; }
	[JsonPropertyName ("token")]
	public required string Token { get; init; }
	[JsonPropertyName ("scope")]
	public required string Scope { get; init; }
	}
