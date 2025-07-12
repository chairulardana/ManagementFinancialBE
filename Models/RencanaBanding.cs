using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class RencanaBanding
    {
        [Key]
        public int IdRencanaBanding { get; set; }

        [Required]
        public int IdUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string NamaRencana { get; set; } = string.Empty;

        [Required]
        public DateTime TanggalRencana { get; set; }

        [MaxLength(255)]
        public string? Rekomendasi { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
          public virtual ICollection<OpsiBanding> OpsiBandings { get; set; } = new List<OpsiBanding>();
        
        // Relasi baru ke Pengeluaran
        public virtual ICollection<Pengeluaran> Pengeluarans { get; set; } = new List<Pengeluaran>();
    }

}
