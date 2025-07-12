using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    public class KategoriPengeluaran
    {
        [Key]
        public int IdKategoriPengeluaran { get; set; }

        [Required] 
        public string NamaKategori { get; set; } = string.Empty;

        public int? IdUser { get; set; }  // <-- Tambahan supaya terkait ke user yang buat

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
