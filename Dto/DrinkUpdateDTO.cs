namespace TokoKebab.Models.DTO
{
    public class DrinkUpdateDTO
    {
        public string NamaMinuman { get; set; } = string.Empty;
        public decimal Harga { get; set; }
        public string Suhu { get; set; } = string.Empty;
        public int Stock { get; set; }
        public string? Image { get; set; }
    }
}
