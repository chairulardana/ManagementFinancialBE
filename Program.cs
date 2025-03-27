using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AuthAPI.Data;
using AuthAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Menambahkan logging
builder.Services.AddLogging();

// Menambahkan konfigurasi koneksi ke database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// Menambahkan pengaturan JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is missing in configuration.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

// Menambahkan autentikasi JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;  // Nonaktifkan untuk pengembangan lokal
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,   // Menonaktifkan validasi issuer
            ValidateAudience = false, // Menonaktifkan validasi audience
            ValidateLifetime = true   // Mengaktifkan validasi lifetime token
        };
    })
    .AddCookie("Cookies", options => 
    {
        options.LoginPath = "/api/auth/login";  // Rute untuk login
        options.LogoutPath = "/api/auth/logout";  // Rute untuk logout
        options.Cookie.HttpOnly = true;  // Cookie hanya bisa diakses oleh HTTP
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Hanya dikirim melalui HTTPS
        options.Cookie.SameSite = SameSiteMode.Strict;  // Menghindari CSRF
        options.ExpireTimeSpan = TimeSpan.FromDays(1);  // Durasi cookie
    });

builder.Services.AddAuthorization();

// Menambahkan CORS untuk mengizinkan semua asal (Origin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; // Jika frontend berjalan di domain yang berbeda
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.LoginPath = "/"; // Pastikan ini sesuai dengan route login kamu
});


// Menambahkan Swagger untuk dokumentasi API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KenZonuTss API",
        Version = "v1",
        Description = "Login API with JWT Authentication"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT token in the format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Menambahkan layanan untuk UserService
builder.Services.AddScoped<IUserService, UserService>();

// Menambahkan layanan untuk Controller API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

// Menambahkan Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// Menambahkan konfigurasi CORS
app.UseCors("AllowAll");

// Menambahkan autentikasi dan otorisasi
app.UseAuthentication();
app.UseAuthorization();

// Menambahkan routing untuk controller
app.MapControllers();

app.Run();
