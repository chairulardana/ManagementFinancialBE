using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TokoKebab.Models
{
    public class DashboardStats
    {
        [Key]
        public DateTime Tanggal { get; set; } // Primary Key (tanggal laporan)

        public int TotalTransactions { get; set; } // Jumlah total transaksi
        public decimal TotalPemasukan { get; set; } // Total pemasukan
        public int TotalCustomers { get; set; } // Jumlah pelanggan unik

        // Relasi ke TopSellingProduct
        public int? TopSellingProductId { get; set; } // Foreign key
        public TopSellingProduct? TopSellingProduct { get; set; }

        // Relasi ke SalesPerHour
        public List<SalesPerHour>? SalesPerHours { get; set; }
    }
}
