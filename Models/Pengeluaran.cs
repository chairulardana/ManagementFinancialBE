using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class Pengeluaran
    {
        [Key]
        public int IdPengeluaran { get; set; }

        [Required]
        public int IdUser { get; set; }

        [Required]
        public DateOnly Tanggal { get; set; }

        public string Deskripsi { get; set; } = string.Empty;

        [Required]
        public decimal Nominal { get; set; }

        public int IdKategoriPengeluaran { get; set; }

        [ForeignKey("IdKategoriPengeluaran")]
        public KategoriPengeluaran KategoriPengeluaran { get; set; }

        public int? IdRencanaBanding { get; set; }

        [ForeignKey("IdRencanaBanding")]
        public virtual RencanaBanding? RencanaBanding { get; set; }

        // Tambahan untuk setor tabungan
        public int? IdTargetTabungan { get; set; }

        [ForeignKey("IdTargetTabungan")]
        public virtual TargetTabungan? TargetTabungan { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
