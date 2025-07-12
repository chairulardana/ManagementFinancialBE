using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class OpsiBanding
    {
        [Key]
        public int IdOpsiBanding { get; set; }

        [Required]
        public int IdRencanaBanding { get; set; }

        [ForeignKey("IdRencanaBanding")]
        public RencanaBanding? RencanaBanding { get; set; }

        [Required]
        [MaxLength(100)]
        public string NamaOpsi { get; set; } = string.Empty;

        [Required]
        public decimal EstimasiBiaya { get; set; }

        public int? IdKategoriPengeluaran { get; set; }

        [ForeignKey("IdKategoriPengeluaran")]
        public KategoriPengeluaran? KategoriPengeluaran { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
