using Microsoft.AspNetCore.Mvc;
using AuthAPI.Models;
using AuthAPI.Data;
using AuthAPI.DTOs;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace AuthAPI.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(AuthDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound("User tidak ditemukan.");
            }
            return Ok(user);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromForm] UserDto userDto, IFormFile? imageFile)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound("User tidak ditemukan.");
            }

            // Update nama, email, dan role (tanpa validasi role tertentu)
            user.Name = userDto.Name;
            user.Email = userDto.Email;
            user.Role = userDto.Role; // Role bisa apa saja (tidak dibatasi)

            // Update password jika ada
            if (!string.IsNullOrEmpty(userDto.Password))
            {
                user.Password_Hash = _passwordHasher.HashPassword(user, userDto.Password);
            }

            // Handle upload gambar (sama seperti sebelumnya)
            string uploadsFolder = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string imagesFolder = Path.Combine(uploadsFolder, "uploads");

            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);
            }

            if (imageFile != null)
            {
                var allowedExtensions = new List<string> { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension) || !imageFile.ContentType.StartsWith("image/"))
                {
                    return BadRequest("Hanya file gambar (JPG, JPEG, PNG) yang diperbolehkan.");
                }

                string uniqueFileName = $"{id}_{Path.GetFileNameWithoutExtension(imageFile.FileName)}{fileExtension}";
                string filePath = Path.Combine(imagesFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                user.image = $"/uploads/{uniqueFileName}";
            }

            _context.Users.Update(user);
            _context.SaveChanges();
            return Ok(new { message = "User berhasil diperbarui.", user });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound("User tidak ditemukan.");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();
            return Ok("User berhasil dihapus.");
        }
    }
}