using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class SetoranTargetTabungan
    {
        [Key]
        public int IdSetoran { get; set; }

        [Required]
        public int IdTargetTabungan { get; set; }

        [ForeignKey("IdTargetTabungan")]
        public TargetTabungan? TargetTabungan { get; set; }

        [Required]
        public decimal NominalSetor { get; set; }

        [Required]
        public DateTime Tanggal { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
