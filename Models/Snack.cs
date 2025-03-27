using System.ComponentModel.DataAnnotations;

public class Snack
{
    [Key]
    public int Id_Snack { get; set; }

    [Required]
    public string Nama_Snack { get; set; } = string.Empty;

    [Required]
    public decimal Harga { get; set; }

    [Required]
    public int Stock { get; set; }
    
    public string? Image { get; set; }
}
