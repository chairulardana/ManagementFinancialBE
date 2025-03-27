namespace TokoKebab.DTO
{
    public class KebabUpdateDTO
    {
        public string NamaKebab { get; set; } = string.Empty;
        public decimal Harga { get; set; }  // Ubah ke decimal
        public string Size { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Stock { get; set; }
        public string? Image { get; set; }
    }

}
