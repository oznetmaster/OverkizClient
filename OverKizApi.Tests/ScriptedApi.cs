// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

// Every request is intercepted. An unexpected request fails instead of reaching a network.
internal sealed class ScriptedApi : IDisposable
	{
	internal const string DeviceUrl = "io://test-gateway/shutter#2";
	internal const string EncodedDeviceUrl = "io%3A%2F%2Ftest-gateway%2Fshutter%232";
	internal const string CloudEndpoint = "https://overkiz.example.invalid/api/";
	private readonly Handler _handler = new ();
	internal HttpClient Http
		{
		get;
		}
	internal OverkizClient Client
		{
		get;
		}
	internal int RequestCount => _handler.RequestCount;
	internal bool HandlerDisposed => _handler.Disposed;

	internal ScriptedApi (OverkizServer? server = null, string? token = "synthetic-token")
		{
		Http = new HttpClient (_handler);
		Client = new OverkizClient ("tester@example.invalid", "synthetic-password",
			 server ?? new OverkizServer { Name = "Test cloud", Endpoint = CloudEndpoint, Manufacturer = "Test" }, token, Http);
		}

	internal void Expect (string method, string path, string response = "{}", HttpStatusCode status = HttpStatusCode.OK,
		 System.Action<HttpRequestMessage, string>? inspect = null)
		 => _handler.Enqueue (method, new Uri (Http.BaseAddress!, path).AbsoluteUri, response, status, inspect);

	internal void Complete ()
		{
		Assert.That (_handler.Failures, Is.Empty, "A request assertion was swallowed by library error handling.");
		Assert.That (_handler.Pending, Is.Zero, "Expected HTTP requests were not made.");
		}
	public void Dispose () => Http.Dispose ();

	internal static JsonElement Json (string text)
		{
		using var document = JsonDocument.Parse (text);
		return document.RootElement.Clone ();
		}

	internal static Dictionary<string, string> Form (string text) => text.Split ('&')
		 .Select (pair => pair.Split (new[] { '=' }, 2))
		 .ToDictionary (pair => Decode (pair[0]), pair => Decode (pair[1]));
	private static string Decode (string text) => Uri.UnescapeDataString (text.Replace ("+", " "));

	private sealed class Handler : HttpMessageHandler
		{
		private readonly Queue<Func<HttpRequestMessage, string, HttpResponseMessage>> _steps = new ();
		internal int Pending => _steps.Count;
		internal List<string> Failures { get; } = new ();
		internal int RequestCount
			{
			get; private set;
			}
		internal bool Disposed
			{
			get; private set;
			}

		internal void Enqueue (string method, string uri, string response, HttpStatusCode status,
			 System.Action<HttpRequestMessage, string>? inspect)
			{
			_steps.Enqueue ((request, body) =>
			{
				Assert.That (request.Method.Method, Is.EqualTo (method));
				Assert.That (request.RequestUri!.AbsoluteUri, Is.EqualTo (uri));
				inspect?.Invoke (request, body);
				return new HttpResponseMessage (status) { Content = new StringContent (response, Encoding.UTF8, "application/json") };
			});
			}

		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
			{
			cancellationToken.ThrowIfCancellationRequested ();
			RequestCount++;
			try
				{
				if (_steps.Count == 0)
					throw new AssertionException ($"Unexpected HTTP request: {request.Method} {request.RequestUri}");
				string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync ().ConfigureAwait (false);
				return _steps.Dequeue () (request, body);
				}
			catch (AssertionException exception)
				{
				Failures.Add (exception.Message);
				throw;
				}
			}

		protected override void Dispose (bool disposing)
			{
			Disposed = true;
			base.Dispose (disposing);
			}
		}
	}