using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Models
{
    public class User
    {
        [Key]
        public int Id_User { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password_Hash { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? image { get; set; }
    }

}
