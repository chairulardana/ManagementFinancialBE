public class CreateTargetTabunganRequest
{
    public string NamaTarget { get; set; } = string.Empty;
    public string Deskripsi { get; set; } = string.Empty;
    public decimal NominalTarget { get; set; }
    public DateTime? TanggalTarget { get; set; }
    
    // Menambahkan properti untuk gambar
    public string? Gambar { get; set; }  // Nullable, bisa null jika tidak ada gambar
}
