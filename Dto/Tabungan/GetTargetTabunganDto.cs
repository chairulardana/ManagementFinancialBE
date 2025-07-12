public class TargetTabunganResponse
{
    public int IdTargetTabungan { get; set; }
    public string NamaTarget { get; set; } = string.Empty;
    public string Deskripsi { get; set; } = string.Empty;
    public decimal NominalTarget { get; set; }
    public decimal NominalTerkumpul { get; set; }
    public DateTime TanggalMulai { get; set; }
    public DateTime? TanggalTarget { get; set; }
    public string Status { get; set; } = string.Empty;

    // Menambahkan properti untuk gambar
    public string? Gambar { get; set; }  // Nullable, bisa null jika tidak ada gambar
}
