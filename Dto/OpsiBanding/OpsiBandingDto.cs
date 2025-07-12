public class OpsiBandingDto
{
    public int IdOpsiBanding { get; set; }
    public string NamaOpsi { get; set; } = string.Empty;
     public int? IdKategoriPengeluaran { get; set; }
    public decimal EstimasiBiaya { get; set; }
    public string? NamaKategori { get; set; } // diambil dari tabel KategoriPengeluaran
}