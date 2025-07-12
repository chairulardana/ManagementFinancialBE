using AuthAPI.Data;
using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OpsiBandingController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public OpsiBandingController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/OpsiBanding?rencanaBandingId=1
        [HttpGet]
        public async Task<IActionResult> GetOpsiBanding(int rencanaBandingId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Pastikan RencanaBanding milik user ini
            var rencana = await _context.RencanaBandings
                .FirstOrDefaultAsync(rb => rb.IdRencanaBanding == rencanaBandingId && rb.IdUser == userId);

            if (rencana == null)
                return NotFound("RencanaBanding tidak ditemukan.");

            var data = await _context.OpsiBandings
                .Where(ob => ob.IdRencanaBanding == rencanaBandingId)
                .Select(ob => new OpsiBandingDto
                {
                    IdOpsiBanding = ob.IdOpsiBanding,
                    NamaOpsi = ob.NamaOpsi,
                    EstimasiBiaya = ob.EstimasiBiaya,
                    IdKategoriPengeluaran = ob.IdKategoriPengeluaran,
                    NamaKategori = _context.KategoriPengeluarans
                        .Where(k => k.IdKategoriPengeluaran == ob.IdKategoriPengeluaran)
                        .Select(k => k.NamaKategori)
                        .FirstOrDefault() ?? string.Empty
                }).ToListAsync();

            return Ok(data);
        }















        // POST: api/OpsiBanding
        [HttpPost]
public async Task<IActionResult> CreateOpsiBanding(OpsiBandingCreateDto request)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Pastikan RencanaBanding milik user ini
    var rencana = await _context.RencanaBandings
        .FirstOrDefaultAsync(rb => rb.IdRencanaBanding == request.IdRencanaBanding && rb.IdUser == userId);

    if (rencana == null)
        return NotFound("RencanaBanding tidak ditemukan.");

    // Cari IdKategoriPengeluaran berdasarkan NamaKategori
    int? kategoriId = null;
    if (!string.IsNullOrEmpty(request.NamaKategori))
    {
        var kategori = await _context.KategoriPengeluarans
            .FirstOrDefaultAsync(k => k.NamaKategori == request.NamaKategori);

        if (kategori != null)
        {
            kategoriId = kategori.IdKategoriPengeluaran;
        }
        else
        {
            return BadRequest($"Kategori '{request.NamaKategori}' tidak ditemukan.");
        }
    }

    var opsi = new OpsiBanding
    {
        IdRencanaBanding = request.IdRencanaBanding,
        NamaOpsi = request.NamaOpsi,
        EstimasiBiaya = request.EstimasiBiaya,
        IdKategoriPengeluaran = kategoriId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.OpsiBandings.Add(opsi);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Opsi banding berhasil ditambahkan." });
}

    }
}
