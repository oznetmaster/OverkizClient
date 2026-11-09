// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Exceptions;

/// <summary>Credentials were rejected by the Brandt/Sauter Smart Control middleware.</summary>
/// <remarks>Initialises an error without including credentials, cookies or token response contents.</remarks>
/// <param name="message">The non-secret error description.</param>
public class BrandtBadCredentialsException (string message) : BadCredentialsException (message)
	{
	}

/// <summary>The Brandt/Sauter middleware is unavailable or returned an invalid JWT response.</summary>
/// <remarks>Initialises an error without including credentials, cookies or token response contents.</remarks>
/// <param name="message">The non-secret error description.</param>
public class BrandtServiceException (string message) : BaseOverkizException (message)
	{
	}