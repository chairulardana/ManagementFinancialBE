public class RencanaBandingCreateDto
{
    public string NamaRencana { get; set; } = string.Empty;
    public DateTime TanggalRencana { get; set; }
    public List<OpsiBandingCreateDto> OpsiBanding { get; set; } = new();
}
