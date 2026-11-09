# Brandt and Sauter Smart Control

For Sauter Smart Control appliances, select `Server.SauterSmartControl`. For Sauter Cozytouch heating gateways, keep `Server.SauterCozytouch`; their identity services and accounts are different.

```csharp
await using var client = new OverkizClient(username, password,
    OverkizConst.SupportedServers[Server.SauterSmartControl]);
bool connected = await client.Login(registerEventListener: true);
```

For Brandt, select `Server.Brandt`. Both use the Smart Control middleware's cookie-session and JWT flow, with different partner identifiers. Passwords are sent only to the middleware session endpoint; the cloud login receives the JWT alone. Keep credentials outside source control and application logs.

The default HTTP transport supports cookie sessions. When supplying an HttpClient, use a cookie-enabled handler and a client dedicated to this Overkiz instance. Do not share DefaultRequestHeaders with another service. The application owns and disposes its supplied HttpClient.

Catch `BrandtBadCredentialsException` for middleware credential rejection and `BrandtServiceException` for middleware service/JWT failures. Both are Overkiz exception subtypes; they apply to Sauter as well as Brandt. Final cloud-login failures retain the usual Overkiz exception mapping. Errors never include credential-bearing middleware bodies.

`Login(false)` skips listener registration. Successful login clears cached setup/device/gateway/listener state. Sign-in waits for admitted operations, and later calls wait for the sign-in transition. Independent reads can overlap. Await disposal of the library client before disposing its supplied transport; callers must coordinate any mutations of returned models.

Brandt/Sauter protocol validation uses fake credentials and intercepted responses. Existing live local-gateway tests exercise a different authentication path; they do not establish successful live Smart Control login.
