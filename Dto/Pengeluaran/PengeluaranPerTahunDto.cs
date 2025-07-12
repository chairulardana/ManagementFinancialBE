namespace AuthAPI.DTOs
{
   public class PengeluaranPerTahunDto
{
    public int Tahun { get; set; }
    public decimal PersentasePengeluaran { get; set; }
      public decimal TotalPengeluaran { get; set; }
    public KategoriTerbesarDto KategoriTerbesar { get; set; }
}

}
