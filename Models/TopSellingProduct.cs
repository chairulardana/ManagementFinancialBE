using System;
using System.ComponentModel.DataAnnotations;

namespace TokoKebab.Models
{
    public class TopSellingProduct
    {
        [Key]
        public int Id { get; set; } // Primary Key

        public string ProductName { get; set; } = string.Empty; // Nama produk terlaris
        public int UnitsSold { get; set; } // Jumlah unit terjual
        public decimal Revenue { get; set; } // Total pendapatan produk terlaris
        public DateTime ReportDate { get; set; } // Tanggal laporan
    }
}
