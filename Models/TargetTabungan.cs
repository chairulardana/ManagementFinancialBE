using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class TargetTabungan
    {
        [Key]
        public int IdTargetTabungan { get; set; }

        [Required]
        public int IdUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string NamaTarget { get; set; } = string.Empty;

        public string Deskripsi { get; set; } = string.Empty;

        [Required]
        public decimal NominalTarget { get; set; }

        [Required]
        public decimal NominalTerkumpul { get; set; } = 0;

        public DateTime TanggalMulai { get; set; } = DateTime.UtcNow;

        public DateTime? TanggalTarget { get; set; }  // Bisa null kalau target waktu tidak wajib
         public string? Gambar { get; set; } 

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Sedang Menabung";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relasi ke User (foreign key)
        [ForeignKey(nameof(IdUser))]
        public virtual User? User { get; set; }

        // Koleksi setoran terkait target tabungan
        public virtual ICollection<SetoranTargetTabungan> SetoranTargetTabungans { get; set; } = new List<SetoranTargetTabungan>();

        // Koleksi pengeluaran terkait target tabungan
        public virtual ICollection<Pengeluaran> Pengeluarans { get; set; } = new List<Pengeluaran>();
    }
}
