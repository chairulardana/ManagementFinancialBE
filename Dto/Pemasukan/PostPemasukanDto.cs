namespace AuthAPI.DTOs
{
    public class CreatePemasukanDto
    {
        public DateTime Tanggal { get; set; }
        public string Deskripsi { get; set; } = string.Empty;
        public decimal Jumlah { get; set; }
    }
}
