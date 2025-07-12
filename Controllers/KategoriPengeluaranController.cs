using AuthAPI.Data;
using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Biar hanya user login yang bisa akses
    public class KategoriPengeluaranController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public KategoriPengeluaranController(AuthDbContext context)
        {
            _context = context;
        }

[HttpGet]
public async Task<ActionResult<IEnumerable<KategoriPengeluaranDto>>> GetKategoriPengeluaran()
{
    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var userId = int.Parse(userIdStr);

    var data = await _context.KategoriPengeluarans
        .Where(x => x.IdUser == null || x.IdUser == userId)
        .Select(x => new KategoriPengeluaranDto
        {
            NamaKategori = x.NamaKategori,
        })
        .ToListAsync();

    return Ok(data);
}


[HttpPost]
public async Task<ActionResult> CreateKategori(KategoriPengeluaranCreateDto dto)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Validasi format nama
    if (!Regex.IsMatch(dto.NamaKategori, @"^[a-zA-Z\s]+$"))
    {
        return BadRequest("Nama kategori hanya boleh berisi huruf dan spasi.");
    }

    // Validasi kata kasar
    var forbiddenWords = new[] { "anjing", "babi", "bangsat", "kontol", "memek", "pepek", "peler", "titit", "pentil", "monyet", "goblok", "tolol", "kampret", "tai", "sialan", "fuck", "bitch", "shit", "bastard", "cunt", "asshole" };
    var namaLower = dto.NamaKategori.ToLower();

    if (forbiddenWords.Any(forbidden => Regex.IsMatch(namaLower, $@"\b{forbidden}\b", RegexOptions.IgnoreCase)))
    {
        return BadRequest("Nama kategori mengandung kata yang tidak pantas.");
    }

    // Cek di SEMUA kategori (baik milik user maupun default)
    var existingCategory = await _context.KategoriPengeluarans
        .Where(x => x.NamaKategori.ToLower() == dto.NamaKategori.ToLower())
        .FirstOrDefaultAsync();

    // Kategori sudah ada di sistem
    if (existingCategory != null)
    {
        // Kategori default (NULL) tidak perlu ditambahkan ke user
        if (existingCategory.IdUser == null)
        {
            return Ok(new { 
                message = "Kategori default sudah tersedia. Anda dapat langsung menggunakannya.",
                isDefault = true
            });
        }
        
        // Kategori milik user lain atau sudah ada di user ini
        return BadRequest("Kategori sudah ada di profil Anda.");
    }

    // Buat kategori BARU khusus user
    var kategori = new KategoriPengeluaran
    {
        IdUser = userId, // Selalu milik user
        NamaKategori = dto.NamaKategori,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.KategoriPengeluarans.Add(kategori);
    await _context.SaveChangesAsync();

    return Ok(new {
        message = "Kategori berhasil ditambahkan.",
        kategoriId = kategori.IdKategoriPengeluaran
    });
}

[HttpDelete("{id}")]
public async Task<ActionResult> DeleteKategori(int id)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Cari kategori
    var kategori = await _context.KategoriPengeluarans
        .FirstOrDefaultAsync(x => x.IdKategoriPengeluaran == id);

    if (kategori == null)
        return NotFound("Kategori tidak ditemukan.");

    // Cegah hapus kategori default
    if (kategori.IdUser == null)
        return BadRequest("Kategori default tidak bisa dihapus.");

    // Pastikan hanya pemilik yang menghapus
    if (kategori.IdUser != userId)
        return Forbid();

    // **Pre‐check: ada pengeluaran di kategori ini?**
    bool hasPengeluaran = await _context.Pengeluarans
        .AnyAsync(p => p.IdKategoriPengeluaran == id);

    if (hasPengeluaran)
    {
        return BadRequest("Tidak bisa menghapus kategori ini karena masih ada transaksi pengeluaran.");
    }

    // Safe to delete
    _context.KategoriPengeluarans.Remove(kategori);
    await _context.SaveChangesAsync();

    return NoContent();
}

    }
}
