// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace OverKizApi.Models;

/// <summary>A preset slot advertised by the device's core:SupportedAliases attribute.</summary>
public sealed class SupportedAlias
	{
	/// <summary>Identifier to supply to the device's goToAlias command.</summary>
	[JsonPropertyName ("id")]
	public required string Id { get; init; }
	/// <summary>Vendor alias type, including unknown types introduced by newer devices.</summary>
	[JsonPropertyName ("type")]
	public required string Type { get; init; }
	/// <summary>Features supported by this slot, in source order.</summary>
	[JsonPropertyName ("features")]
	public IReadOnlyList<string> Features { get; init; } = [];
	}

public sealed partial class Device
	{
	/// <summary>Returns all valid advertised alias slots in source order, including repeated types.</summary>
	/// <remarks>
	/// Missing or non-list attributes return an empty list. Malformed entries are skipped.
	/// IDs may be nonempty strings or integer values; types and features are strings.
	/// Missing features are treated as an empty list. Unknown alias types are preserved.
	/// </remarks>
	public IReadOnlyList<SupportedAlias> GetSupportedAliases ()
		{
		if (Attributes["core:SupportedAliases"]?.Value is not IReadOnlyList<object?> values)
			return [];
		var aliases = new List<SupportedAlias> ();
		foreach (var value in values)
			{
			if (value is not IReadOnlyDictionary<string, object?> entry
				|| !entry.TryGetValue ("id", out var rawId)
				|| !entry.TryGetValue ("type", out var rawType)
				|| rawType is not string type || string.IsNullOrWhiteSpace (type))
				continue;
			var id = rawId is string text ? text
				: rawId is sbyte or byte or short or ushort or int or uint or long or ulong
					? Convert.ToString (rawId, CultureInfo.InvariantCulture) : null;
			if (string.IsNullOrWhiteSpace (id)) continue;
			var features = new List<string> ();
			if (entry.TryGetValue ("features", out var rawFeatures))
				{
				if (rawFeatures is not IReadOnlyList<object?> featureValues || featureValues.Any (feature => feature is not string))
					continue;
				features.AddRange (featureValues.Cast<string> ());
				}
			aliases.Add (new SupportedAlias { Id = id!, Type = type, Features = features.AsReadOnly () });
			}
		return aliases.AsReadOnly ();
		}

	/// <summary>Returns one alias per type, selecting the slot with the most features; source order breaks ties.</summary>
	public IReadOnlyDictionary<string, SupportedAlias> GetMostFeaturedAliases ()
		{
		var aliases = new Dictionary<string, SupportedAlias> (StringComparer.Ordinal);
		foreach (var alias in GetSupportedAliases ())
			if (!aliases.TryGetValue (alias.Type, out var current) || alias.Features.Count > current.Features.Count)
				aliases[alias.Type] = alias;
		return new System.Collections.ObjectModel.ReadOnlyDictionary<string, SupportedAlias> (aliases);
		}
	}