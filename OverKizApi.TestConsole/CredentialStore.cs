// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OverKizApi.Enums;

namespace OverKizApi.TestConsole;

/// <summary>Persisted credentials for a single server.</summary>
internal sealed class SavedCredential
	{
	[JsonPropertyName ("username")]
	public string Username { get; set; } = string.Empty;

	[JsonPropertyName ("password")]
	public string Password { get; set; } = string.Empty;
	}

/// <summary>
/// Loads and saves per-server credentials to a JSON file next to the executable
/// so repeated test runs don't require re-entering credentials each time.
/// </summary>
internal static class CredentialStore
	{
	private static readonly string _filePath = Path.Combine (
		AppContext.BaseDirectory, "test-credentials.json");

	private static readonly JsonSerializerOptions _jsonOptions = new ()
		{
		WriteIndented = true,
		};

	private const string LOCAL_KEY = "__local__";

	// ── Public API ────────────────────────────────────────────────────────

	/// <summary>
	/// Returns saved credentials for <paramref name="server"/>, or <see langword="null"/>
	/// if none have been saved yet.
	/// </summary>
	public static SavedCredential? Load (Server server)
		{
		Dictionary<string, SavedCredential> store = ReadFile ();
		_ = store.TryGetValue (server.ToString (), out SavedCredential? cred);
		return cred;
		}

	/// <summary>Persists <paramref name="cred"/> for <paramref name="server"/>.</summary>
	public static void Save (Server server, SavedCredential cred)
		{
		Dictionary<string, SavedCredential> store = ReadFile ();
		store [server.ToString ()] = cred;
		File.WriteAllText (_filePath, JsonSerializer.Serialize (store, _jsonOptions));
		}

	/// <summary>
	/// Returns the saved local-connection entry (gateway IP in <c>Username</c>, token in <c>Password</c>),
	/// or <see langword="null"/> if none have been saved yet.
	/// </summary>
	public static SavedCredential? LoadLocal ()
		{
		Dictionary<string, SavedCredential> store = ReadFile ();
		_ = store.TryGetValue (LOCAL_KEY, out SavedCredential? cred);
		return cred;
		}

	/// <summary>Persists a local-connection entry (gateway IP + token).</summary>
	public static void SaveLocal (SavedCredential cred)
		{
		Dictionary<string, SavedCredential> store = ReadFile ();
		store [LOCAL_KEY] = cred;
		File.WriteAllText (_filePath, JsonSerializer.Serialize (store, _jsonOptions));
		}

	// ── Helpers ───────────────────────────────────────────────────────────

	private static Dictionary<string, SavedCredential> ReadFile ()
		{
		if (!File.Exists (_filePath))
			return [];

		try
			{
			string json = File.ReadAllText (_filePath);
			return JsonSerializer.Deserialize<Dictionary<string, SavedCredential>> (json, _jsonOptions)
				?? [];
			}
		catch
			{
			return [];
			}
		}
	}
