// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class EventConcurrencyTests
	{
	[TestCase (false)]
	[TestCase (true)]
	public async Task OverlappingPolls_ShareRegistrationAndRefresh (bool expiredToken)
		{
		using var handler = new BlockingHandler (expiredToken);
		using var http = new HttpClient (handler);
		var server = expiredToken ? OverkizConst.SupportedServers[Server.SomfyEurope]
			: new OverkizServer { Name = "Offline", Endpoint = ScriptedApi.CloudEndpoint, Manufacturer = "Offline" };
		var client = new OverkizClient ("synthetic@example.invalid", "synthetic", server, "synthetic", http);
		if (expiredToken) await client.Login ();
		Task<IReadOnlyList<EventObject>> first = client.FetchEvents (true);
		Task<IReadOnlyList<EventObject>>? second = null;
		try
			{
			Assert.That (await Task.WhenAny (handler.Blocked.Task, Task.Delay (5000)), Is.SameAs (handler.Blocked.Task));
			second = client.FetchEvents (true);
			Assert.That (handler.Registrations, Is.EqualTo (1));
			Assert.That (handler.Tokens, Is.EqualTo (expiredToken ? 2 : 0));
			}
		finally
			{
			handler.Release.TrySetResult (true);
			}
		var complete = Task.WhenAll (first, second!);
		Assert.That (await Task.WhenAny (complete, Task.Delay (5000)), Is.SameAs (complete));
		await complete;
		Assert.That (handler.Registrations, Is.EqualTo (expiredToken ? 2 : 1));
		Assert.That (handler.Tokens, Is.EqualTo (expiredToken ? 2 : 0));
		Assert.That (handler.FetchedIds, Is.EqualTo (new[] { expiredToken ? "two" : "one", expiredToken ? "two" : "one" }));
		await client.DisposeAsync ();
		}

	private sealed class BlockingHandler (bool expireToken) : HttpMessageHandler
		{
		internal readonly TaskCompletionSource<bool> Blocked = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal readonly TaskCompletionSource<bool> Release = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal readonly List<string> FetchedIds = [];
		internal int Registrations;
		internal int Tokens;
		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
			{
			var path = request.RequestUri!.AbsolutePath;
			if (path.EndsWith ("/token/jwt", StringComparison.Ordinal))
				{
				int count = Interlocked.Increment (ref Tokens);
				if (count == 2)
					{
					Blocked.TrySetResult (true);
					await Release.Task;
					}
				return Response ("{\"access_token\":\"synthetic\",\"refresh_token\":\"synthetic\",\"expires_in\":" + (count == 1 ? 0 : 3600) + "}");
				}
			if (path.EndsWith ("/register", StringComparison.Ordinal))
				{
				int count = Interlocked.Increment (ref Registrations);
				if (!expireToken && count == 1)
					{
					Blocked.TrySetResult (true);
					await Release.Task;
					}
				return Response (count == 1 ? "{\"id\":\"one\"}" : "{\"id\":\"two\"}");
				}
			if (path.EndsWith ("/fetch", StringComparison.Ordinal))
				{
				FetchedIds.Add (path.Contains ("/one/") ? "one" : "two");
				return Response ("[]");
				}
			if (path.EndsWith ("/unregister", StringComparison.Ordinal)) return Response ("{}");
			throw new InvalidOperationException ("Unexpected offline request: " + path);
			}
		private static HttpResponseMessage Response (string content)
			=> new (HttpStatusCode.OK) { Content = new StringContent (content, Encoding.UTF8, "application/json") };
		}
	}