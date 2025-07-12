using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AuthAPI.Models;
using AuthAPI.Data;
using AuthAPI.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;

using AuthAPI.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace AuthAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;
        private readonly IDistributedCache _cache;
            private readonly EmailService _emailService;

        public AuthController(
            AuthDbContext context,
            IConfiguration configuration,
            IDistributedCache cache,
               TokenService tokenService,
            EmailService emailService
        )
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
            _tokenService = tokenService;
              _emailService = emailService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            // Cek email sudah terdaftar
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
                return BadRequest(new { message = "Email sudah terdaftar." });

            // Regex validasi password kuat
            var strongPasswordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");

            if (!strongPasswordRegex.IsMatch(userDto.Password))
                return BadRequest(new
                {
                    message = "Password harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol."
                });

            // Simpan user ke database
            var user = new User
            {
                NamaLengkap = userDto.NamaLengkap,
                Email = userDto.Email,
                Password = _passwordHasher.HashPassword(null, userDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Langsung login setelah register
            return await Login(new LoginDto
            {
                Identifier = userDto.Email,
                Password = userDto.Password
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            string cacheKey = $"user:{loginDto.Identifier}";
            string cachedUserJson = await _cache.GetStringAsync(cacheKey);

            User? user = null;

            if (!string.IsNullOrEmpty(cachedUserJson))
            {
                user = JsonSerializer.Deserialize<User>(cachedUserJson);
            }
            else
            {
                user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Email == loginDto.Identifier || u.NamaLengkap == loginDto.Identifier);

                if (user != null)
                {
                    var userJson = JsonSerializer.Serialize(user);
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                    };
                    await _cache.SetStringAsync(cacheKey, userJson, options);
                }
            }

            if (user == null || string.IsNullOrEmpty(user.Password))
                return Unauthorized(new { message = "Email/Nama atau password salah." });

            var result = _passwordHasher.VerifyHashedPassword(null, user.Password, loginDto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Email/Nama atau password salah." });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login berhasil",
                token = token,
                user = new
                {
                    Id = user.IdUser,
                    Name = user.NamaLengkap,
                    Email = user.Email
                }
            });
        }
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
                new Claim(ClaimTypes.Name, user.NamaLengkap),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            try
            {
                // Verifikasi token Google
                var payload = await _tokenService.VerifyGoogleTokenAsync(googleLoginDto.Credential);

                // Pastikan payload valid
                if (payload == null)
                    return Unauthorized(new { message = "Token Google tidak valid." });

                // Cek apakah pengguna ada di database, jika tidak, buat pengguna baru
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null)
                {
                    // Pengguna tidak ditemukan, buat pengguna baru
                    user = new User
                    {
                        NamaLengkap = payload.Name,
                        Email = payload.Email,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Setelah Google Token terverifikasi, generate JWT Token untuk aplikasi
                // Gunakan ID pengguna yang ada di database untuk klaim NameIdentifier
                var token = _tokenService.GenerateTokenWithGoogle(payload, user.IdUser);

                return Ok(new
                {
                    message = "Login berhasil",
                    token = token, // JWT Token yang valid
                    user = new
                    {
                        Id = user.IdUser,  // Pastikan ID yang digunakan di aplikasi sama dengan ID di database
                        Name = user.NamaLengkap,
                        Email = user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Gagal login dengan Google", error = ex.Message });
            }
        }



        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string token)
        {
            try
            {
                // Validate the token received from Google
                var payload = await GoogleJsonWebSignature.ValidateAsync(token);

                return payload;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public class GoogleLoginDto
        {
            public string Credential { get; set; }
        }
      [HttpPost("reset-password-request")]
public async Task<IActionResult> ResetPasswordRequest([FromBody] ResetPasswordRequestDto requestDto)
{
    // Cek apakah email ada di database
    var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == requestDto.Email);
    if (user == null)
        return BadRequest(new { message = "Email tidak terdaftar." });

    // Generate OTP sementara (6 digit angka)
    var otp = new Random().Next(100000, 999999).ToString();

    // Simpan OTP di cache dengan waktu kedaluwarsa (misalnya 10 menit)
    var cacheKey = $"reset-password-otp:{requestDto.Email}";
    var options = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)  // OTP berlaku selama 5 menit
    };
    await _cache.SetStringAsync(cacheKey, otp, options);

    // Baca template HTML dari file
    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "reset_password_template.html");
    var template = await System.IO.File.ReadAllTextAsync(templatePath);

    // Ganti placeholder {{otp}} dengan OTP yang telah dihasilkan
    var body = template.Replace("{{otp}}", otp);

    // Subjek email
    var subject = "Reset Password OTP";

    // Kirim email
    await _emailService.SendEmailAsync(requestDto.Email, subject, body);

    return Ok(new { message = "OTP untuk reset password telah dikirim ke email Anda." });
}

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OTP otpDto)
        {
            // Verifikasi OTP yang ada dalam cache
            var cacheKey = $"reset-password-otp:{otpDto.Email}";
            var cachedOtp = await _cache.GetStringAsync(cacheKey);

            if (cachedOtp == null || cachedOtp != otpDto.Otp)
                return BadRequest(new { message = "OTP tidak valid atau telah kedaluwarsa." });

            return Ok(new { message = "OTP valid. Sekarang, silakan masukkan password baru." });
        }
        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] RestPassword resetDto)
        {
            // Cek apakah email ada di database
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == resetDto.Email);
            if (user == null)
                return BadRequest(new { message = "Email tidak terdaftar." });

            // Validasi password kuat
            var strongPasswordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
            if (!strongPasswordRegex.IsMatch(resetDto.Password))
                return BadRequest(new
                {
                    message = "Password harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol."
                });

            // Hash password baru dan simpan
            user.Password = _passwordHasher.HashPassword(null, resetDto.Password);
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Hapus OTP dari cache setelah digunakan
            var cacheKey = $"reset-password-otp:{resetDto.Email}";
            await _cache.RemoveAsync(cacheKey);

            return Ok(new { message = "Password berhasil diperbarui." });
        }
[HttpPost("resend-otp")]
public async Task<IActionResult> ResendOtp([FromBody] ResetPasswordRequestDto requestDto)
{
    // Cek email terdaftar
    var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == requestDto.Email);
    if (user == null)
        return BadRequest(new { message = "Email tidak terdaftar." });

    // Generate OTP baru
    var newOtp = new Random().Next(100000, 999999).ToString();
    
    // Update OTP di cache
    var cacheKey = $"reset-password-otp:{requestDto.Email}";
    var options = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };
    await _cache.SetStringAsync(cacheKey, newOtp, options);

    // Kirim email dengan OTP baru
    await _emailService.SendEmailAsync(
        requestDto.Email,
        "Reset Password OTP (Baru)",
        $"Kode OTP baru Anda adalah: {newOtp}"
    );

    return Ok(new { message = "OTP baru telah dikirim ke email Anda." });
}
// DTO untuk reset password request (meminta OTP)
public class ResetPasswordRequestDto
{
    public string Email { get; set; }
}
// DTO untuk menerima OTP yang dimasukkan pengguna
public class OTP
{
    public string Email { get; set; }  // Email pengguna yang terkait dengan OTP
    public string Otp { get; set; }  // OTP yang diterima pengguna
}
// DTO untuk reset password setelah memasukkan OTP
public class RestPassword
{
    public string Email { get; set; }  // Email pengguna yang terkait dengan reset password
    public string Password { get; set; }  // Password baru yang akan diset
}



    }
}
