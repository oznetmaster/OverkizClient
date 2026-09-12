// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverKizApi.TestSupport;

/// <summary>Private runtime inputs shared by the console and opt-in local API tests.</summary>
internal sealed class LiveTestSettings
	{
	[JsonPropertyName ("enabled")]
	public bool Enabled { get; set; }

	[JsonPropertyName ("gatewayHost")]
	public string GatewayHost { get; set; } = string.Empty;

	[JsonPropertyName ("token")]
	public string Token { get; set; } = string.Empty;

	[JsonPropertyName ("timeoutSeconds")]
	public int TimeoutSeconds { get; set; } = 15;
	}

/// <summary>Stores local test inputs outside source checkouts and build outputs.</summary>
internal static class LiveTestSettingsStore
	{
	internal static string DefaultPath => Path.Combine (
		Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "OverkizClient", "LiveTestSettings.json");

	private static readonly JsonSerializerOptions Options = new () { WriteIndented = true, PropertyNameCaseInsensitive = true };

	internal static LiveTestSettings? Load (string path)
		{
		if (!File.Exists (path))
			return null;
		try
			{
			return JsonSerializer.Deserialize<LiveTestSettings> (File.ReadAllText (path), Options);
			}
		catch (JsonException)
			{
			// Do not include credential-bearing JSON in diagnostics.
			throw new InvalidDataException ("LiveTestSettings.json is not a valid settings object.");
			}
		}

	internal static void SaveLocalCredentials (string host, string token, string? path = null)
		{
		path ??= DefaultPath;
		LiveTestSettings settings = Load (path) ?? new ();
		settings.GatewayHost = host;
		settings.Token = token;
		string directory = Path.GetDirectoryName (Path.GetFullPath (path))!;
		Directory.CreateDirectory (directory);
		File.WriteAllText (path, JsonSerializer.Serialize (settings, Options));
		}
	}