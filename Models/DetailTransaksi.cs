using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthAPI.Models;

namespace TokoKebab.Models
{
    public class DetailTransaksi
    {
        [Key]
        public int Id_DetailTransaksi { get; set; }

        [ForeignKey("User")]
        public int  Id_User   { get; set; }

        public virtual User? User { get; set; }

        [ForeignKey("Drink")]
        public int? Id_Drink { get; set; }
        public virtual Drink? Drink { get; set; }

        [ForeignKey("Kebab")]
        public int? Id_Kebab { get; set; }
        public virtual Kebab? Kebab { get; set; }

        [ForeignKey("Snack")]
        public int? Id_Snack { get; set; }
        public virtual Snack? Snack { get; set; }

        [ForeignKey("PaketMakanan")]
        public int? Id_Paket { get; set; }
        public virtual PaketMakanan? PaketMakanan { get; set; }

        [Required]
        public DateTime TanggalTransaksi { get; set; }

        [Required]
        public int Jumlah { get; set; }

        [Required]
        public decimal TotalHarga { get; set; }}
    }

