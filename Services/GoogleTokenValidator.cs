using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace AuthAPI.Services
{
    public class GoogleTokenValidator
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleTokenValidator> _logger;

        public GoogleTokenValidator(
            RequestDelegate next, 
            IConfiguration configuration, 
            ILogger<GoogleTokenValidator> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for anonymous endpoints
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                try
                {
                    var payload = await GoogleJsonWebSignature.ValidateAsync(
                        token,
                        new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[] { "401819278020-ffjibof25ld8mbl3vsahtrm7jl2h1vl2.apps.googleusercontent.com" }
                        });

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, payload.Subject),
                        new Claim(ClaimTypes.Name, payload.Name),
                        new Claim(ClaimTypes.Email, payload.Email),
                        new Claim("picture", payload.Picture),
                        new Claim("google_id", payload.Subject)
                    };

                    var identity = new ClaimsIdentity(claims, "Google");
                    context.User = new ClaimsPrincipal(identity);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Google token validation failed: {ex.Message}");
                    // Jangan hentikan request, biarkan dilanjutkan tanpa autentikasi
                }
            }

            await _next(context);
        }
    }
}