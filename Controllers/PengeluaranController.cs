using AuthAPI.Data;
using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text.Json;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PengeluaranController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IDatabase _redis;

        public PengeluaranController(AuthDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis.GetDatabase();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PengeluaranGetDto>>> GetPengeluarans()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cacheKey = $"pengeluarans:{userId}";

            // Cek apakah ada cache di Redis
            var cachedData = await _redis.StringGetAsync(cacheKey);
            if (cachedData.HasValue)
            {
                var cachedPengeluarans = JsonSerializer.Deserialize<List<PengeluaranGetDto>>(cachedData);
                return Ok(cachedPengeluarans);
            }

            var pengeluarans = await _context.Pengeluarans
                .Where(p => p.IdUser == userId)
                .Include(p => p.KategoriPengeluaran)
                .Include(p => p.TargetTabungan)
                .Select(p => new PengeluaranGetDto
                {
                    IdPengeluaran = p.IdPengeluaran,
                    Tanggal = p.Tanggal,
                    Deskripsi = p.Deskripsi,
                    Nominal = p.Nominal,
                    IdKategoriPengeluaran = p.IdKategoriPengeluaran,
                    NamaKategori = p.KategoriPengeluaran.NamaKategori,
                    IdTargetTabungan = p.IdTargetTabungan,
                    NamaTarget = p.TargetTabungan != null ? p.TargetTabungan.NamaTarget : string.Empty
                })
                .ToListAsync();

            // Simpan ke Redis cache dengan TTL 5 menit
            await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(pengeluarans), TimeSpan.FromMinutes(5));

            return Ok(pengeluarans);
        }
[HttpPost]
public async Task<ActionResult> CreatePengeluaran(PengeluaranCreateDto dto)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    var totalPemasukan = await _context.Pemasukans
        .Where(p => p.IdUser == userId)
        .SumAsync(p => (decimal?)p.Jumlah) ?? 0;

    var totalPengeluaran = await _context.Pengeluarans
        .Where(p => p.IdUser == userId)
        .SumAsync(p => (decimal?)p.Nominal) ?? 0;

    var saldoSaatIni = totalPemasukan - totalPengeluaran;

    // Pastikan saldo mencukupi
    if (dto.Nominal > saldoSaatIni)
    {
        return BadRequest("Saldo tidak mencukupi.");
    }

    // Periksa apakah kategori sudah ada di profil pengguna
    var kategori = await _context.KategoriPengeluarans
        .FirstOrDefaultAsync(k => k.NamaKategori.ToLower() == dto.NamaKategori.ToLower() && k.IdUser == userId);

    if (kategori == null)
    {
        // Periksa apakah kategori yang dipilih adalah kategori default (IdUser == null)
        var defaultKategori = await _context.KategoriPengeluarans
            .FirstOrDefaultAsync(k => k.NamaKategori.ToLower() == dto.NamaKategori.ToLower() && k.IdUser == null);

        if (defaultKategori != null)
        {
            // Jika kategori adalah kategori default, kita tidak menambahkannya ke profil pengguna.
            kategori = defaultKategori; // Cukup gunakan kategori default untuk pengeluaran, tanpa menambahkannya ke profil pengguna.
        }
        else
        {
            // Jika kategori tidak ada baik di profil pengguna maupun kategori default, maka buat kategori baru untuk pengguna
            kategori = new KategoriPengeluaran
            {
                IdUser = userId,
                NamaKategori = dto.NamaKategori,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.KategoriPengeluarans.Add(kategori);
            await _context.SaveChangesAsync();
        }
    }

    // Membuat pengeluaran baru
    var pengeluaran = new Pengeluaran
    {
        IdUser = userId,
        Tanggal = DateOnly.FromDateTime(DateTime.UtcNow),
        Deskripsi = dto.Deskripsi,
        Nominal = dto.Nominal,
        IdKategoriPengeluaran = kategori.IdKategoriPengeluaran,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.Pengeluarans.Add(pengeluaran);
    await _context.SaveChangesAsync();

    // Hapus cache Redis agar data baru langsung muncul
    var cacheKey = $"pengeluarans:{userId}";
    await _redis.KeyDeleteAsync(cacheKey);

    return Ok(new { message = "Pengeluaran berhasil dibuat." });
}


        [HttpGet("total-pengeluaran-per-tahun")]
public async Task<ActionResult> GetTotalPengeluaranPerTahun(int tahun)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Nama tahun yang dipilih
    var totalPengeluaran = await _context.Pengeluarans
        .Where(p => p.IdUser == userId && p.Tanggal.Year == tahun)
        .SumAsync(p => (decimal?)p.Nominal) ?? 0;

    // Format tahun (contoh: "2025")
    var tahunResponse = $"{tahun}";

    return Ok(new {TotalPengeluaran = totalPengeluaran });
}


        [HttpGet("total-pengeluaran-bulan")]
public async Task<ActionResult> GetTotalPengeluaranBulan([FromQuery] int bulan, [FromQuery] int tahun)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

   
    if (bulan < 1 || bulan > 12)
    {
        return BadRequest("Bulan tidak valid. Harus antara 1 dan 12.");
    }


            var bulanNama = new[] { "Januari", "Februari", "Maret", "April", "Mei", "Juni", 
                           "Juli", "Agustus", "September", "Oktober", "November", "Desember" };

    // Ambil total pengeluaran untuk bulan dan tahun saat ini
    var totalPengeluaran = await _context.Pengeluarans
        .Where(p => p.IdUser == userId && p.Tanggal.Year == tahun && p.Tanggal.Month == bulan)
        .SumAsync(p => (decimal?)p.Nominal) ?? 0;

    // Format bulan dan tahun (contoh: "Juli, 2025")
    var bulanTahun =$"{bulanNama[bulan - 1]}, {tahun}";

    return Ok(new { BulanTahun = bulanTahun, TotalPengeluaran = totalPengeluaran });
}

[HttpGet("chart-per-bulan")]
public async Task<ActionResult<IEnumerable<PengeluaranPerBulanDto>>> GetPengeluaranPerBulan(int tahun)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Ambil semua data pengeluaran untuk user dan tahun tertentu, termasuk kategori
    var pengeluarans = await _context.Pengeluarans
        .Where(p => p.IdUser == userId && p.Tanggal.Year == tahun)
        .Include(p => p.KategoriPengeluaran)
        .ToListAsync();

    // Siapkan list untuk 12 bulan
    var result = new List<PengeluaranPerBulanDto>();

    for (int bulan = 1; bulan <= 12; bulan++)
    {
        // Filter pengeluaran per bulan
        var pengeluaranBulanIni = pengeluarans.Where(p => p.Tanggal.Month == bulan).ToList();

        var totalPengeluaranBulan = pengeluaranBulanIni.Sum(p => p.Nominal);

        // Group by kategori di bulan ini
        var perKategori = pengeluaranBulanIni
            .GroupBy(p => p.IdKategoriPengeluaran)
            .Select(g => new PengeluaranPerKategoriDto
            {
                NamaKategori = g.First().KategoriPengeluaran?.NamaKategori ?? "Unknown",
                TotalPengeluaranKategori = g.Sum(p => p.Nominal),
                // FIXED: Added missing closing parenthesis
                PersentasePengeluaran = totalPengeluaranBulan != 0 ? 
                    (g.Sum(p => p.Nominal) / totalPengeluaranBulan * 100) : 0
            })
            .ToList();

        result.Add(new PengeluaranPerBulanDto
        {
            Bulan = bulan,
            TotalPengeluaran = totalPengeluaranBulan,
            KategoriPengeluaran = perKategori
        });
    }

    return Ok(result);
}

  [HttpGet("chart-per-tahun")]
        public async Task<ActionResult<IEnumerable<PengeluaranPerTahunDto>>> GetPengeluaranPerTahun()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Get all expenses with categories
            var pengeluarans = await _context.Pengeluarans
                .Where(p => p.IdUser == userId)
                .Include(p => p.KategoriPengeluaran)
                .ToListAsync();

            var totalPengeluaranKeseluruhan = pengeluarans.Sum(p => p.Nominal);

            // Group by year
            var result = pengeluarans
                .GroupBy(p => p.Tanggal.Year)
                .Select(g => new
                {
                    Tahun = g.Key,
                    TotalPengeluaran = g.Sum(p => p.Nominal),
                    KategoriGroups = g
                        .GroupBy(p => p.IdKategoriPengeluaran)
                        .Select(kg => new
                        {
                            NamaKategori = kg.First().KategoriPengeluaran?.NamaKategori ?? "Unknown",
                            TotalKategori = kg.Sum(p => p.Nominal)
                        })
                        .ToList()
                })
                .OrderBy(x => x.Tahun)
                .ToList();

            var response = result.Select(r => {
                var topCategory = r.KategoriGroups
                    .OrderByDescending(kg => kg.TotalKategori)
                    .FirstOrDefault();

                return new PengeluaranPerTahunDto
                {
                    Tahun = r.Tahun,
                    TotalPengeluaran = r.TotalPengeluaran,
                    PersentasePengeluaran = totalPengeluaranKeseluruhan > 0 ? 
                        (r.TotalPengeluaran / totalPengeluaranKeseluruhan * 100) : 0,
                    KategoriTerbesar = topCategory != null ? new KategoriTerbesarDto
                    {
                        NamaKategori = topCategory.NamaKategori,
                        PersentaseKategori = r.TotalPengeluaran > 0 ? 
                            (topCategory.TotalKategori / r.TotalPengeluaran * 100) : 0
                    } : null
                };
            }).ToList();

            return Ok(response);
        }

    }
}
