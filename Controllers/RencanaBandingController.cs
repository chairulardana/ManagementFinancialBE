using AuthAPI.Data;
using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RencanaBandingController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public RencanaBandingController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRencanaBanding(RencanaBandingCreateDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                return Unauthorized(new { success = false, message = "Akun pengguna tidak valid" });

            // Hitung saldo real-time
            var totalPemasukan = await _context.Pemasukans
                .Where(p => p.IdUser == userId)
                .SumAsync(p => (decimal?)p.Jumlah) ?? 0;

            var totalPengeluaran = await _context.Pengeluarans
                .Where(p => p.IdUser == userId)
                .SumAsync(p => (decimal?)p.Nominal) ?? 0;

            var saldoSaatIni = totalPemasukan - totalPengeluaran;

            var totalEstimasi = request.OpsiBanding.Sum(ob => ob.EstimasiBiaya);

            // Cek kecukupan saldo
            if (saldoSaatIni < totalEstimasi)
            {
                var sekarang = DateTime.UtcNow;
                var bulanDepan = new DateTime(sekarang.Year, sekarang.Month, 1).AddMonths(1);

                if (request.TanggalRencana.Month == bulanDepan.Month &&
                    request.TanggalRencana.Year == bulanDepan.Year)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Saldo Anda saat ini tidak mencukupi untuk rencana banding bulan depan"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Saldo Anda tidak mencukupi"
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var rencana = new RencanaBanding
                {
                    IdUser = userId,
                    NamaRencana = request.NamaRencana,
                    TanggalRencana = request.TanggalRencana,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.RencanaBandings.Add(rencana);
                await _context.SaveChangesAsync();

                foreach (var opsi in request.OpsiBanding)
                {
                    int? kategoriId = null;
                    if (!string.IsNullOrEmpty(opsi.NamaKategori))
                    {
                        var kategori = await _context.KategoriPengeluarans
                            .FirstOrDefaultAsync(k => k.NamaKategori == opsi.NamaKategori);

                        if (kategori == null)
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Kategori '{opsi.NamaKategori}' tidak ditemukan"
                            });

                        kategoriId = kategori.IdKategoriPengeluaran;
                    }

                    _context.OpsiBandings.Add(new OpsiBanding
                    {
                        IdRencanaBanding = rencana.IdRencanaBanding,
                        NamaOpsi = opsi.NamaOpsi,
                        EstimasiBiaya = opsi.EstimasiBiaya,
                        IdKategoriPengeluaran = kategoriId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
                rencana.Rekomendasi = GenerateSafetyRecommendation(userId, totalEstimasi, request.OpsiBanding);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Rencana banding berhasil dibuat",
                    data = new
                    {
                        rencana.IdRencanaBanding,
                        rencana.NamaRencana,
                        rencana.TanggalRencana,
                        rekomendasi = rencana.Rekomendasi
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "Terjadi kesalahan internal: " + ex.Message
                });
            }
        }

        private string GenerateSafetyRecommendation(int userId, decimal totalEstimasi, List<OpsiBandingCreateDto> opsiBanding)
        {
            var sb = new StringBuilder();

            var pengeluarans = _context.Pengeluarans
                .Where(p => p.IdUser == userId && p.IdKategoriPengeluaran != null)
                .Include(p => p.KategoriPengeluaran)
                .ToList();

            if (pengeluarans.Any())
            {
                var rataPengeluaran = pengeluarans.Average(p => p.Nominal);
                if (totalEstimasi > rataPengeluaran * 1.2m)
                {
                    sb.Append("Perhatian: Estimasi biaya melebihi batas pengeluaran rata-rata Anda. ");
                }
            }

            // 2. Rekomendasi berdasarkan opsi yang dipilih
            if (opsiBanding.Count == 1)
            {
                var opsi = opsiBanding.First();
                sb.Append($"Fokus pada opsi '{opsi.NamaOpsi}'. Estimasi biaya: Rp {opsi.EstimasiBiaya:N0}. Cari alternatif penyedia untuk mendapatkan harga terbaik.");
            }
            else if (opsiBanding.Count > 1)
            {
                var termurah = opsiBanding.OrderBy(o => o.EstimasiBiaya).First();
                var termahal = opsiBanding.OrderBy(o => o.EstimasiBiaya).Last();
                decimal hemat = termahal.EstimasiBiaya - termurah.EstimasiBiaya;

                sb.Append($"Prioritaskan opsi '{termurah.NamaOpsi}' (dapat menghemat hingga Rp {hemat:N0} per transaksi). ");
                if (opsiBanding.Count > 2)
                {
                    sb.Append($"Hindari opsi '{termahal.NamaOpsi}' karena biaya lebih tinggi.");
                }
            }
            else
            {
                sb.Append("Saran: Bandingkan minimal tiga opsi dan lakukan negosiasi harga.");
            }

            return sb.ToString();
        }

        // GET: api/RencanaBanding
        [HttpGet]
        public async Task<IActionResult> GetRencanaBanding()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var data = await _context.RencanaBandings
                .Where(rb => rb.IdUser == userId)
                .Include(rb => rb.OpsiBandings)
                .Select(rb => new RencanaBandingDto
                {
                    IdRencanaBanding = rb.IdRencanaBanding,
                    NamaRencana = rb.NamaRencana,
                    TanggalRencana = rb.TanggalRencana,
                    Rekomendasi = rb.Rekomendasi,
                    OpsiBanding = rb.OpsiBandings.Select(ob => new OpsiBandingDto
                    {
                        IdOpsiBanding = ob.IdOpsiBanding,
                        NamaOpsi = ob.NamaOpsi,
                        EstimasiBiaya = ob.EstimasiBiaya,
                        IdKategoriPengeluaran = ob.IdKategoriPengeluaran,
                        NamaKategori = _context.KategoriPengeluarans
                            .Where(k => k.IdKategoriPengeluaran == ob.IdKategoriPengeluaran)
                            .Select(k => k.NamaKategori)
                            .FirstOrDefault() ?? string.Empty
                    }).ToList()
                }).ToListAsync();

            return Ok(new { success = true, data });
        }

        // DELETE: api/RencanaBanding/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRencanaBanding(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var rencana = await _context.RencanaBandings
                .Include(r => r.OpsiBandings)
                .FirstOrDefaultAsync(r => r.IdRencanaBanding == id && r.IdUser == userId);

            if (rencana == null)
                return NotFound(new { success = false, message = "Rencana tidak ditemukan" });

            _context.RencanaBandings.Remove(rencana);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Rencana berhasil dihapus" });
        }
    }
}

