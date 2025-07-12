using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
   public class PengeluaranCreateDto
{
    public string Deskripsi { get; set; } = string.Empty;
    public decimal Nominal { get; set; }
    public string NamaKategori { get; set; } = string.Empty;
}

}
