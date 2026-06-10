namespace BengalTex.ERP.Api.Middleware;

/// <summary>
/// Stamps a small set of conservative security headers on every response.
///
/// Rationale (Hardening Increment 2): the API serves JSON to a SPA on a separate origin;
/// there is no need to allow inline scripts, framing, or any active content. Swagger UI
/// (dev only) needs looser CSP so it is detected by path and exempted from CSP — other
/// headers still apply.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // OnStarting fires just before the status line + headers are sent. Required so
        // we don't try to mutate headers after the response body has begun writing
        // (which throws InvalidOperationException for streamed/chunked responses).
        context.Response.OnStarting(() =>
        {
            void SetIfAbsent(string name, string value)
            {
                if (!headers.ContainsKey(name)) headers[name] = value;
            }

            SetIfAbsent("X-Content-Type-Options", "nosniff");
            SetIfAbsent("X-Frame-Options", "DENY");
            SetIfAbsent("Referrer-Policy", "strict-origin-when-cross-origin");
            SetIfAbsent("X-Permitted-Cross-Domain-Policies", "none");

            // Allow geolocation for GPS attendance check-in; block camera/mic/payment by default.
            SetIfAbsent("Permissions-Policy", "geolocation=(self), camera=(), microphone=(), payment=()");

            // Strip the IIS/ASP.NET fingerprint headers if the host added them.
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");

            // CSP — strict for API JSON; relaxed for Swagger UI (which needs inline css/js).
            var path = context.Request.Path.Value ?? string.Empty;
            var isSwagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
            if (isSwagger)
            {
                SetIfAbsent("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; " +
                    "font-src 'self' data:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'");
            }
            else
            {
                SetIfAbsent("Content-Security-Policy",
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'");
            }

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
