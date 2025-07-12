using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuthAPI.Data;
using AuthAPI.Models;
using AuthAPI.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PemasukanController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IDistributedCache _cache;

        public PemasukanController(AuthDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Endpoint untuk mendapatkan saldo total pemasukan
        [HttpGet("saldo")]
        public async Task<IActionResult> GetSaldo()
        {
            // Ambil userId dari klaim NameIdentifier
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Periksa apakah klaim NameIdentifier valid
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            // Jika Anda menggunakan 'long' untuk IdUser di database, pastikan konversinya benar
            if (!long.TryParse(userIdString, out long userId))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            // Proses saldo dan pengeluaran berdasarkan userId
            var totalPemasukan = await _context.Pemasukans
                .Where(p => p.IdUser == userId)
                .SumAsync(p => (decimal?)p.Jumlah) ?? 0;

            var totalPengeluaran = await _context.Pengeluarans
                .Where(p => p.IdUser == userId)
                .SumAsync(p => (decimal?)p.Nominal) ?? 0;

            var saldo = totalPemasukan - totalPengeluaran;
            var formattedSaldo = saldo.ToString("N0");

            return Ok(new { Saldo = formattedSaldo });
        }



        [HttpGet("total-pemasukan-per-tahun")]
        public async Task<ActionResult> GetTotalPemasukanPerTahun(int tahun)
        {
            // Gunakan long dan TryParse untuk mengambil userId
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdString, out long userId))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            var totalPemasukan = await _context.Pemasukans
                .Where(p => p.IdUser == userId && p.Tanggal.Year == tahun)
                .SumAsync(p => (decimal?)p.Jumlah) ?? 0;

            var tahunResponse = $"{tahun}";

            return Ok(new { Tahun = tahunResponse, TotalPemasukan = totalPemasukan });
        }

        [HttpGet("total-pemasukan-bulan")]
        public async Task<ActionResult> GetTotalPemasukanBulan([FromQuery] int bulan, [FromQuery] int tahun)
        {
            // Gunakan long dan TryParse untuk mengambil userId
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdString, out long userId))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            if (bulan < 1 || bulan > 12)
            {
                return BadRequest("Bulan tidak valid. Harus antara 1 dan 12.");
            }

            var bulanNama = new[] { "Januari", "Februari", "Maret", "April", "Mei", "Juni",
                                   "Juli", "Agustus", "September", "Oktober", "November", "Desember" };

            var totalPemasukan = await _context.Pemasukans
                .Where(p => p.IdUser == userId && p.Tanggal.Year == tahun && p.Tanggal.Month == bulan)
                .SumAsync(p => (decimal?)p.Jumlah) ?? 0;

            var bulanTahun = $"{bulanNama[bulan - 1]}, {tahun}";

            return Ok(new { BulanTahun = bulanTahun, TotalPemasukan = totalPemasukan });
        }

        [HttpGet]
        public async Task<IActionResult> GetPemasukan()
        {
            // Gunakan long dan TryParse untuk mengambil userId
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdString, out long userId))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            string cacheKey = $"pemasukan:{userId}";

            string cachedPemasukan = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedPemasukan))
            {
                var cachedData = JsonSerializer.Deserialize<List<Pemasukan>>(cachedPemasukan);
                return Ok(cachedData);
            }

            var pemasukanList = await _context.Pemasukans
                .Where(p => p.IdUser == userId)
                .OrderByDescending(p => p.Tanggal)
                .ToListAsync();

            var jsonData = JsonSerializer.Serialize(pemasukanList);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, jsonData, options);

            return Ok(pemasukanList);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePemasukan([FromBody] CreatePemasukanDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Gunakan long dan TryParse untuk mengambil userId
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User tidak ditemukan");

            var pemasukan = new Pemasukan
            {
                IdUser = (int)userId,
                Tanggal = dto.Tanggal,
                Deskripsi = dto.Deskripsi,
                Jumlah = dto.Jumlah,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Pemasukans.Add(pemasukan);
                await _context.SaveChangesAsync();

                // Invalidate (hapus) cache agar GET berikutnya ambil data terbaru
                await _cache.RemoveAsync($"pemasukan:{userId}");
                await _cache.RemoveAsync($"saldo:{userId}");

                var responseDto = new GetPemasukanDto
                {
                    IdPemasukan = pemasukan.IdPemasukan,
                    NamaLengkap = user.NamaLengkap,
                    Tanggal = pemasukan.Tanggal,
                    Deskripsi = pemasukan.Deskripsi,
                    Jumlah = pemasukan.Jumlah
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                // Log error (jika ada logging framework)
                return StatusCode(500, new { message = "Terjadi kesalahan saat menyimpan data pemasukan", error = ex.Message });
            }
        }
    }
}
