using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TokoKebab.Models
{
    [NotMapped]
    public class DailyPemasukan
    {
        // Hanya bagian tanggal dari TanggalTransaksi yang dipakai
        public DateTime Date { get; set; }
        
        // Jumlah transaksi pada hari tersebut (jumlah baris data transaksi)
        public int TotalTransactions { get; set; }
        
        // Total pemasukan pada hari tersebut (hasil penjumlahan total harga dari DetailTransaksi)
        public decimal TotalPemasukan { get; set; }
    }
}
