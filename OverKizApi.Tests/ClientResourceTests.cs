// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class ClientResourceTests
	{
	[Test]
	public async Task OwnedHttpClient_IsDisposedWithClient ()
		{
		var client = new OverkizClient ("synthetic-user", "synthetic-password",
			 new OverkizServer { Name = "Test", Endpoint = ScriptedApi.CloudEndpoint, Manufacturer = "Test" });
		await client.DisposeAsync ();
		await client.DisposeAsync ();
		// HttpClient rejects the call before sending; this test cannot contact the placeholder host.
		await Assert.ThatAsync (() => client.GetDevices (), Throws.TypeOf<ObjectDisposedException> ());
		}

	[Test]
	public async Task PatchExtension_PreservesContentAndReturnsResponse ()
		{
		using var api = new ScriptedApi ();
		using var content = new StringContent ("{\"enabled\":true}", Encoding.UTF8, "application/json");
		api.Expect ("PATCH", "resource", "{\"updated\":true}", inspect: (request, body) =>
		{
			Assert.That (body, Is.EqualTo ("{\"enabled\":true}"));
			Assert.That (request.Content!.Headers.ContentType!.MediaType, Is.EqualTo ("application/json"));
		});
		using HttpResponseMessage response = await HttpClientExtensions.PatchAsync (api.Http, "resource", content);
		Assert.That (await response.Content.ReadAsStringAsync (), Is.EqualTo ("{\"updated\":true}"));
		api.Complete ();
		}

	[Test]
	public async Task PatchExtension_PropagatesCancellationBeforeSending ()
		{
		using var api = new ScriptedApi ();
		using var cancellation = new CancellationTokenSource ();
		cancellation.Cancel ();
		await Assert.ThatAsync (() => HttpClientExtensions.PatchAsync (api.Http, "resource", null, cancellation.Token),
			 Throws.InstanceOf<OperationCanceledException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}

	[Test]
	public void LocalServerDescriptor_UsesDeveloperApiEndpoint ()
		{
		OverkizServer server = OverkizConst.LocalServer ("gateway.example.invalid");
		Assert.That (server.Endpoint, Is.EqualTo ("https://gateway.example.invalid:8443/enduser-mobile-web/1/enduserAPI/"));
		using var api = new ScriptedApi (server);
		Assert.That (api.Client.ApiType, Is.EqualTo (APIType.Local));
		Assert.That (api.Client.Server, Is.SameAs (server));
		}
	}