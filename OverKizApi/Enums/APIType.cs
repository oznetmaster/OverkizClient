// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Enums;

/// <summary>Determines whether the client communicates with the Overkiz cloud or a local gateway.</summary>
public enum APIType
	{
	/// <summary>Communicate via the Overkiz cloud API (default for all named servers).</summary>
	Cloud,
	/// <summary>Communicate directly with a local gateway using the developer-mode API token.</summary>
	Local,
	}
