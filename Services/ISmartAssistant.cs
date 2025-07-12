using AuthAPI.Data;
using Microsoft.EntityFrameworkCore;

public class SmartAssistantServiceGenZAngry
{
    private readonly AuthDbContext _context;

    public SmartAssistantServiceGenZAngry(AuthDbContext context)
    {
        _context = context;
    }

  public async Task<SaranKeuanganDto> GenerateSmartSaranAngryGenZAsync(int idUser)
{
    var pemasukan = await _context.Pemasukans
        .Where(p => p.IdUser == idUser)
        .SumAsync(p => p.Jumlah);

    var pengeluaranTotal = await _context.Pengeluarans
        .Where(p => p.IdUser == idUser)
        .SumAsync(p => p.Nominal);

    var pengeluaranPerKategori = await _context.Pengeluarans
        .Where(p => p.IdUser == idUser)
        .GroupBy(p => p.IdKategoriPengeluaran)
        .Select(g => new
        {
            IdKategori = g.Key,
            Total = g.Sum(p => p.Nominal)
        })
        .OrderByDescending(g => g.Total)
        .ToListAsync();

    string saran = "";

    if (pemasukan == 0)
    {
        saran = "😡 Bro, lu kok belum masukin pemasukan sama sekali? Tambahin dulu dong, males ah!";
    }
    else
    {
        if (pengeluaranTotal > pemasukan)
        {
            var selisih = pengeluaranTotal - pemasukan;
            saran += $"⚠️ YAK! Lu boros parah, udah ngabisin {selisih:C} lebih banyak dari pemasukan! 😤\n";
        }

        if (pengeluaranPerKategori.Any())
        {
            var kategoriTerbesar = pengeluaranPerKategori.First();
            var namaKategori = await _context.KategoriPengeluarans
                .Where(k => k.IdKategoriPengeluaran == kategoriTerbesar.IdKategori)
                .Select(k => k.NamaKategori)
                .FirstOrDefaultAsync();

            decimal persenKategori = pemasukan == 0 
                ? 0 
                : (kategoriTerbesar.Total / pemasukan) * 100;

            // === Tambahan: jika kategori 'Menabung' dan porsinya sudah besar, beri PUJIAN ===
       // === Tambahan: jika kategori 'Menabung' dan porsinya sudah besar, beri PUJIAN ===
if (namaKategori.Equals("Menabung", StringComparison.OrdinalIgnoreCase) && persenKategori >= 30)
{
    saran += $"🎉 GOKIL! Lo udah nyimpen {persenKategori:F1}% dari pemasukan lo buat nabung. Keep it up, gaes! 💯";
}
else
{
    // logika “menampar” biasa
    if (!namaKategori.Equals("Menabung", StringComparison.OrdinalIgnoreCase))
    {
        saran += $"🔥 Kategori paling ngeselin: '{namaKategori}' nyedot {persenKategori:F1}% dari pemasukan lu!\n";
        if (persenKategori >= 30)
        {
            saran += $"💥 Kurangin dong minimal 20% buat '{namaKategori}' bulan depan, jangan gilak! 😠";
        }
    }
}

        }

        if (string.IsNullOrWhiteSpace(saran))
        {
            saran = "😎 YO! Keuangan lu masih oke punya. Pertahankan, dan plis invest sebagian buat masa depan—jangan pelit! 💸✌️";
        }
    }

    return new SaranKeuanganDto { Saran = saran };
}

}
