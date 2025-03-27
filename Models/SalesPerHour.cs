using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TokoKebab.Models
{
    public class SalesPerHour
    {
        public DateTime Tanggal { get; set; } // Tanggal laporan

        public int Hour { get; set; } // Jam (0-23)

        public decimal Sales { get; set; } // Total pemasukan untuk jam tertentu
    }
}
