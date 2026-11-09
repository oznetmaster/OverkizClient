// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using OverKizApi.Concurrency;

namespace OverKizApi;

public sealed partial class OverkizClient
	{
	private async Task<T> RunRead<T> (Func<Task<T>> read)
		{
		bool refreshed = false;
		while (true)
			{
			using AsyncClientGate.Operation operation = await _operations.EnterAsync (exclusive: false).ConfigureAwait (false);
			if (!refreshed && TokenNeedsRefresh)
				{
				// Release before upgrading. The exclusive admission rechecks expiry,
				// so overlapping readers share the first successful refresh.
				operation.Dispose ();
				await RunWrite (() => Task.CompletedTask, refresh: true).ConfigureAwait (false);
				refreshed = true;
				continue;
				}
			T result = await read ().ConfigureAwait (false);
			_operations.CheckRunning ();
			return result;
			}
		}
	private async Task<T> RunWrite<T> (Func<Task<T>> write, bool refresh)
		{
		using AsyncClientGate.Operation operation = await _operations.EnterAsync (exclusive: true).ConfigureAwait (false);
		if (refresh)
			await RefreshTokenIfExpiredCore (replaceListener: true).ConfigureAwait (false);
		T result = await write ().ConfigureAwait (false);
		_operations.CheckRunning ();
		return result;
		}
	private Task<bool> RunWrite (Func<Task> write, bool refresh) => RunWrite (async () =>
		{
			await write ().ConfigureAwait (false);
			return true;
		}, refresh);
	private bool TokenNeedsRefresh => _expiresAt is not null && _refreshToken is not null && _expiresAt <= DateTime.Now;
	}