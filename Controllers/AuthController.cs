using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AuthAPI.Models;
using AuthAPI.Data;
using AuthAPI.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace AuthAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
        private readonly IConfiguration _configuration;

        public AuthController(AuthDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Registrasi pengguna baru dengan role
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {

            // Cek jika email sudah terdaftar
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return BadRequest(new { message = "Email sudah terdaftar." });
            }

            // Validasi password maksimal 8 karakter
            if (userDto.Password.Length < 8)
            {
                return BadRequest(new { message = "Password harus memiliki minimal 8 karakter." });
            }

            // Validasi role
            if (string.IsNullOrEmpty(userDto.Role))
            {
                userDto.Role = "User"; // Default role jika tidak disediakan
            }

            var user = new User
            {
                Name = userDto.Name,
                Email = userDto.Email,
                Password_Hash = _passwordHasher.HashPassword(null, userDto.Password),
                image = null, // Gambar opsional, bisa diisi nanti
                Role = userDto.Role // Menyimpan role dari DTO
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Otomatis login setelah registrasi
            return await Login(new UserDto
            {
                Email = userDto.Email,
                Password = userDto.Password
            });
        }


        // Endpoint login dan menghasilkan JWT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDto userDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == userDto.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Email atau password salah." });
            }

            var result = _passwordHasher.VerifyHashedPassword(null, user.Password_Hash, userDto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Email atau password salah." });
            }

            // Generate JWT Token
            var token = GenerateJwtToken(user);

            // Set JWT in a secure, HttpOnly cookie
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id_User.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

            var claimsIdentity = new ClaimsIdentity(claims, "Bearer");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authenticationProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddDays(1), // Cookie expires in 1 day
                IsPersistent = true // Marks the cookie as persistent
            };

            // Create and add the cookie (secure, HttpOnly, SameSite)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authenticationProperties
            );

            return Ok(new
            {
                message = "Login berhasil",
                Token = token,  // Return JWT Token for API calls
                User = new
                {
                    Id = user.Id_User,
                    Name = user.Name,
                    Email = user.Email,
                    Image = user.image,
                    Role = user.Role // Include role in response
                }
            });
        }

        // Generate JWT Token dengan role dari user
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id_User.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role) // Menggunakan role dari user
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
    }

    public class UserRegisterDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "User"; // Default role
    }
}