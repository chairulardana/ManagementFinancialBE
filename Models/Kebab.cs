using System.ComponentModel.DataAnnotations;

public class Kebab
{
    [Key]
    public int Id_Kebab { get; set; }

    [Required]
    public string Nama_Kebab { get; set; } = string.Empty;

    [Required]
    public decimal Harga { get; set; }

    [Required]
    public string? Size { get; set; }

    [Required]
    public int Level { get; set; }

    [Required]
    public int Stock { get; set; }

    public string? Image { get; set; }
}
