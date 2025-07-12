namespace AuthAPI.DTOs
{
    public class LoginDto
    { public string Identifier { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
