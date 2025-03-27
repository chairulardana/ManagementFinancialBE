using System.ComponentModel.DataAnnotations;

public class Drink
{
    [Key]
    public int Id_Drink { get; set; }

    [Required]
    public string? Nama_Minuman { get; set; }

    [Required]
    public decimal Harga { get; set; }

    [Required]
    public string? Suhu { get; set; }

    [Required]
    public int Stock { get; set; }
    public string? Image { get; set; }
}
