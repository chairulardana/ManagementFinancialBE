using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class Pemasukan
    {
        [Key]
        public int IdPemasukan { get; set; }

        [Required]
        public int IdUser { get; set; }
 
        [ForeignKey("IdUser")]
        public User User { get; set; } = null!;
        [Required]
        public DateTime Tanggal { get; set; }

        public string Deskripsi { get; set; } = string.Empty;
        public decimal Jumlah { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
