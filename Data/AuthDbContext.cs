using Microsoft.EntityFrameworkCore;
using AuthAPI.Models;
using TokoKebab.Models;
using TokoKebab.Dashboard;

namespace AuthAPI.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Kebab> Kebabs { get; set; }
        public DbSet<Snack> Snacks { get; set; }
        public DbSet<Drink> Drinks { get; set; }
        public DbSet<PaketMakanan> PaketMakanans { get; set; }
        public DbSet<DetailTransaksi> DetailTransaksi { get; set; }
        public DbSet<DashboardStats> DashboardStats { get; set; }
        public DbSet<SalesPerHour> SalesPerHour { get; set; }
        public DbSet<TopSellingProduct> TopSellingProducts { get; set; }

        // Override OnModelCreating untuk konfigurasi model
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurasi composite key untuk SalesPerHour
            modelBuilder.Entity<SalesPerHour>()
                .HasKey(sp => new { sp.Tanggal, sp.Hour });

            // Relasi antara SalesPerHour dan DashboardStats
            modelBuilder.Entity<SalesPerHour>()
                .HasOne<DashboardStats>()
                .WithMany(ds => ds.SalesPerHours)
                .HasForeignKey(sp => sp.Tanggal);

            // Relasi antara DashboardStats dan TopSellingProduct
            modelBuilder.Entity<DashboardStats>()
                .HasOne(ds => ds.TopSellingProduct)
                .WithMany()
                .HasForeignKey(ds => ds.TopSellingProductId);

            // Tambahkan konfigurasi lainnya sesuai kebutuhan...
        }

        // Method: Mendapatkan statistik harian
        public DashboardStats? GetStatsByDate(DateTime date)
        {
            var transactions = DetailTransaksi
                .Where(t => t.TanggalTransaksi.Date == date.Date)
                .ToList();

            if (!transactions.Any())
            {
                return null; // Tidak ada transaksi pada tanggal tersebut
            }

            // Tentukan produk terlaris berdasarkan transaksi
            var topSellingProduct = transactions
                .GroupBy(t => new { t.Id_Drink, t.Id_Kebab, t.Id_Snack, t.Id_Paket })
                .OrderByDescending(g => g.Count())
                .Select(g => new
                {
                    ProductName = GetProductName(g.Key.Id_Drink, g.Key.Id_Kebab, g.Key.Id_Snack, g.Key.Id_Paket),
                    UnitsSold = g.Sum(t => t.Jumlah),
                    Revenue = g.Sum(t => t.TotalHarga)
                })
                .FirstOrDefault();

            // Return objek DashboardStats
            return new DashboardStats
            {
                Tanggal = date,
                TotalTransactions = transactions.Count,
                TotalPemasukan = transactions.Sum(t => t.TotalHarga),
                TotalCustomers = transactions.Select(t => t.Id_DetailTransaksi).Distinct().Count(),
                TopSellingProduct = new TopSellingProduct
                {
                    ProductName = topSellingProduct?.ProductName ?? "N/A",
                    UnitsSold = topSellingProduct?.UnitsSold ?? 0,
                    Revenue = topSellingProduct?.Revenue ?? 0
                },
                SalesPerHours = transactions
                    .GroupBy(t => t.TanggalTransaksi.Hour)
                    .Select(g => new SalesPerHour
                    {
                        Hour = g.Key,
                        Sales = g.Sum(t => t.TotalHarga)
                    }).ToList()
            };
        }

        // Method: Mendapatkan nama produk berdasarkan ID
        private string GetProductName(int? drinkId, int? kebabId, int? snackId, int? paketId)
        {
            if (drinkId.HasValue) return Drinks.Find(drinkId)?.Nama_Minuman ?? "Minuman";
            if (kebabId.HasValue) return Kebabs.Find(kebabId)?.Nama_Kebab ?? "Kebab";
            if (snackId.HasValue) return Snacks.Find(snackId)?.Nama_Snack ?? "Snack";
            if (paketId.HasValue) return PaketMakanans.Find(paketId)?.Nama_Paket ?? "Paket Makanan";

            return "Tidak Diketahui";
        }

        // Method: Mendapatkan statistik mingguan
        public List<DashboardStats> GetWeeklyStats()
        {
            var startDate = DateTime.Today.AddDays(-7).Date;

            // Ambil data statistik dari 7 hari terakhir
            return DashboardStats
                .Include(ds => ds.TopSellingProduct) // Muat relasi produk terlaris
                .Where(ds => ds.Tanggal >= startDate)
                .OrderBy(ds => ds.Tanggal)
                .ToList();
        }
    }
}
