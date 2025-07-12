namespace AuthAPI.DTOs
{
    public class UserDto
    {
         public string Identifier { get; set; }
        public string NamaLengkap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

    }
}
