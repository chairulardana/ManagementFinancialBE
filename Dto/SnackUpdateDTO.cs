namespace TokoKebab.DTO
{
    public class SnackUpdateDTO
    {
        public string NamaSnack { get; set; } = string.Empty;
        public decimal Harga { get; set; }
        public int Stock { get; set; }
        public string? Image { get; set; }
    }
}
