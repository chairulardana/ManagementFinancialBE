namespace TokoKebab.Models.DTO
{
    public class GetDetailTransaksiDto
    {
        public int Id_DetailTransaksi { get; set; }
        public int? Id_Drink { get; set; }
        public string? Nama_Drink { get; set; }
        public int? Id_Kebab { get; set; }
        public string? Nama_Kebab { get; set; }
        public int? Id_Snack { get; set; }
        public string? Nama_Snack { get; set; }
        public int? Id_Paket { get; set; }
        public string? Nama_Paket { get; set; }
        public DateTime TanggalTransaksi { get; set; }
        public int Jumlah { get; set; }
        public decimal TotalHarga { get; set; }
    
        public int Id_user { get; set; }
    }
}
