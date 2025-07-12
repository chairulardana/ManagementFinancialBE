public class OpsiBandingCreateDto
{
      public int IdRencanaBanding { get; set; }
    public string NamaOpsi { get; set; } = string.Empty;
    public decimal EstimasiBiaya { get; set; }
    public string NamaKategori { get; set; }  // FK optional
}