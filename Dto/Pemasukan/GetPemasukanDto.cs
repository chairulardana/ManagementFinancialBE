namespace AuthAPI.DTOs
{
    public class GetPemasukanDto
    {
        public int IdPemasukan { get; set; }
        public DateTime Tanggal { get; set; }
        public string Deskripsi { get; set; } = string.Empty;
        public decimal Jumlah { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
         public string NamaLengkap { get; set; } = string.Empty; // Tambahkan ini
    }
}
