namespace TokoKebab.DTOs
{
    public class PaketMakananCreateDto
    {
        public string Nama_Paket { get; set; } = string.Empty;
        public int Id_Kebab { get; set; }
        public int Id_Snack { get; set; }
        public int Id_Drink { get; set; }
        public decimal Diskon { get; set; } = 0;
        public decimal Harga_Paket { get; set; }
        public decimal Harga_Paket_After_Diskon { get; set; }
        public int? Stok { get;  set; }
        public string? image { get; set; }
    }
}
