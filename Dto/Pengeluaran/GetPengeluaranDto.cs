namespace AuthAPI.DTOs
{
    public class PengeluaranGetDto
    {
        public int IdPengeluaran { get; set; }
        public DateOnly Tanggal { get; set; }
        public string Deskripsi { get; set; } = string.Empty;
        public decimal Nominal { get; set; }
        public int IdKategoriPengeluaran { get; set; }
        public string NamaKategori { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? IdTargetTabungan { get; set; }
public string NamaTarget { get; set; } = string.Empty;
    }
}
