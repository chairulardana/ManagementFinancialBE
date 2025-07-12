using Microsoft.EntityFrameworkCore;
using AuthAPI.Models;

namespace AuthAPI.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Pemasukan> Pemasukans { get; set; }
        public DbSet<KategoriPengeluaran> KategoriPengeluarans { get; set; }
        public DbSet<Pengeluaran> Pengeluarans { get; set; }
        public DbSet<RencanaBanding> RencanaBandings { get; set; }
        public DbSet<OpsiBanding> OpsiBandings { get; set; }
        
        public DbSet<TargetTabungan> TargetTabungans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Pengeluaran>()
                .HasOne(p => p.RencanaBanding)
                .WithMany(r => r.Pengeluarans)
                .HasForeignKey(p => p.IdRencanaBanding)
                .OnDelete(DeleteBehavior.SetNull); // biar aman saat rencana dihapus, pengeluaran tidak ikut terhapus

            modelBuilder.Entity<OpsiBanding>()
                .HasOne(o => o.RencanaBanding)
                .WithMany(r => r.OpsiBandings)
                .HasForeignKey(o => o.IdRencanaBanding)
                .OnDelete(DeleteBehavior.Cascade); // tambahkan cascade agar rencana terhapus otomatis opsi ikut hilang

            modelBuilder.Entity<OpsiBanding>()
                .HasOne(o => o.KategoriPengeluaran)
                .WithMany()
                .HasForeignKey(o => o.IdKategoriPengeluaran)
                .OnDelete(DeleteBehavior.SetNull); // biar aman saat kategori dihapus
        }
    }
}
