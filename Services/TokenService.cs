using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthAPI.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;

        }

        // Generate JWT token untuk pengguna biasa
        public string GenerateToken(string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "default_secret_key"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),  // token expired in 15 minutes
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Generate JWT token dengan Google Token
public string GenerateTokenWithGoogle(GoogleJsonWebSignature.Payload payload, long userId)
{
    // Gunakan userId dari database, bukan dari Google
    var claims = new List<Claim>
    {
        // Gunakan ID pengguna yang ada di database
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),  // Gunakan userId yang valid
        new Claim(ClaimTypes.Name, payload.Name),
        new Claim(ClaimTypes.Email, payload.Email),
        new Claim("picture", payload.Picture),
        new Claim("google_id", payload.Subject)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        audience: _config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1), // token expires in 1 hour
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

        public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string googleToken)
        {
            try
            {
                // Verifikasi token Google menggunakan Google API
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken);
                return payload; // Payload akan berisi informasi seperti email dan name
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid Google Token", ex);
            }
        }
        public void ConfigureServices(IServiceCollection services)
        {
            // Menambahkan autentikasi JWT ke pipeline aplikasi
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = _config["Jwt:Issuer"],
                        ValidAudience = _config["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]))
                    };
                });

            // Menambahkan layanan Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
            });

            services.AddControllers();
        }
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Gunakan autentikasi dan otorisasi
    app.UseAuthentication();  // Memverifikasi token
    app.UseAuthorization();   // Menentukan otorisasi untuk setiap permintaan

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}



    }
}
