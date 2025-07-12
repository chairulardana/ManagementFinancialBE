namespace AuthAPI.DTOs
{
public class PengeluaranPerBulanDto
{
    public int Bulan { get; set; }
    public decimal TotalPengeluaran { get; set; }
    public List<PengeluaranPerKategoriDto> KategoriPengeluaran { get; set; }
}

}
