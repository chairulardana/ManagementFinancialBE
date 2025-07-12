public class RencanaBandingDto
{
    public int IdRencanaBanding { get; set; }
    public string NamaRencana { get; set; } = string.Empty;
    public DateTime TanggalRencana { get; set; }
    public string? Rekomendasi { get; set; }
    public List<OpsiBandingDto> OpsiBanding { get; set; } = new();
}