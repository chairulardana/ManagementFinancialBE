using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuthAPI.Data;
using AuthAPI.Models;
using System.Threading.Tasks;
using System;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SetoranController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public SetoranController(AuthDbContext context)
        {
            _context = context;
        }

[HttpPost("setor")]
public async Task<IActionResult> SetorTabungan([FromQuery] string namaTarget, [FromBody] CreateSetoranRequest request)
{
    if (string.IsNullOrEmpty(namaTarget))
        return BadRequest(new { Message = "NamaTarget harus diisi." });

    if (request.NominalSetor <= 0)
        return BadRequest(new { Message = "NominalSetor harus lebih dari 0." });

    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    var target = await _context.TargetTabungans
        .FirstOrDefaultAsync(t => t.NamaTarget == namaTarget && t.IdUser == userId);

    if (target == null)
        return NotFound(new { Message = "Target tabungan tidak ditemukan." });

    // Cek saldo pengguna
    var totalPemasukan = await _context.Pemasukans
        .Where(p => p.IdUser == userId)
        .SumAsync(p => (decimal?)p.Jumlah) ?? 0;

    var totalPengeluaran = await _context.Pengeluarans
        .Where(p => p.IdUser == userId)
        .SumAsync(p => (decimal?)p.Nominal) ?? 0;

    var saldo = totalPemasukan - totalPengeluaran;

    // Cek apakah saldo cukup untuk melakukan setoran
    if (saldo < request.NominalSetor)
    {
        return BadRequest(new { Message = "Saldo tidak cukup untuk melakukan setoran." });
    }

    // Update nominal terkumpul dan status
    target.NominalTerkumpul += request.NominalSetor;
    target.UpdatedAt = DateTime.UtcNow;

    if (target.NominalTerkumpul >= target.NominalTarget)
    {
        target.Status = "Tercapai";
    }
    else
    {
        target.Status = "Sedang Menabung";
    }

    // Cari IdKategoriPengeluaran berdasarkan nama kategori "Menabung"
    var kategori = await _context.KategoriPengeluarans
        .FirstOrDefaultAsync(k => k.NamaKategori == "Menabung");

    if (kategori == null)
        return BadRequest(new { Message = "Kategori 'Menabung' belum tersedia." });

    var pengeluaran = new Pengeluaran
    {
        IdUser = userId,
        Tanggal = DateOnly.FromDateTime(DateTime.UtcNow),
        Deskripsi = target.NamaTarget,
        Nominal = request.NominalSetor,
        IdTargetTabungan = target.IdTargetTabungan,
        IdKategoriPengeluaran = kategori.IdKategoriPengeluaran,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.Pengeluarans.Add(pengeluaran);
    await _context.SaveChangesAsync();

    return Ok(new { Message = "Setoran tabungan berhasil disimpan." });
}

        // GET: api/Setoran/list?idTargetTabungan=xx (optional)
        [HttpGet("list")]
        public async Task<IActionResult> GetSetoranList([FromQuery] int? idTargetTabungan)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var query = _context.Pengeluarans
                .Include(p => p.TargetTabungan)
                .Where(p => p.IdUser == userId && p.IdTargetTabungan != null);

            if (idTargetTabungan.HasValue)
            {
                query = query.Where(p => p.IdTargetTabungan == idTargetTabungan.Value);
            }

            var setoranList = await query.Select(p => new SetoranResponse
            {
                IdSetoran = p.IdPengeluaran,
                IdTargetTabungan = p.IdTargetTabungan!.Value,
                NamaTarget = p.TargetTabungan!.NamaTarget,
                NominalSetor = p.Nominal,
                Tanggal = p.Tanggal.ToDateTime(TimeOnly.MinValue)
            }).ToListAsync();

            return Ok(setoranList);
        }
    }
}
