using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace TokoKebab.Models
{
    public class PaketMakanan
    {
        [Key]
        public int Id_Paket { get; set; }

        [Required]
        public string Nama_Paket { get; set; } = string.Empty;

        [ForeignKey("Kebab")]
        public int Id_Kebab { get; set; }

        [ForeignKey("Snack")]
        public int Id_Snack { get; set; }

        [ForeignKey("Drink")]
        public int Id_Drink { get; set; }

        [Required]
        public decimal Harga_Paket { get; set; }

        public decimal Diskon { get; set; } = 0;

        public decimal Harga_Paket_After_Diskon { get; set; } 

        public virtual Kebab? Kebab { get; set; }
        public virtual Snack? Snack { get; set; }
        public virtual Drink? Drink { get; set; }
        public int? Stok  { get; set; }    

        public string? image { get; set; }
    }
}   
