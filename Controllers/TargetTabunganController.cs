using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuthAPI.Models;
using AuthAPI.Data;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TargetTabunganController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public TargetTabunganController(AuthDbContext context)
        {
            _context = context;
        }

        // POST: api/TargetTabungan
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateTarget([FromForm] CreateTargetTabunganRequest dto, IFormFile? file)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Cek nama target sudah ada untuk user ini atau belum
            bool exists = await _context.TargetTabungans
                .AnyAsync(t => t.IdUser == userId && t.NamaTarget == dto.NamaTarget);
            if (exists)
                return BadRequest(new { message = "NamaTarget sudah ada." });

            // Proses upload gambar jika ada file
            string? imagePath = null;
            if (file != null)
            {
                // Generate nama file yang unik
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                // Pastikan direktori tujuan ada
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Menyimpan file ke folder yang ditentukan
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                imagePath = $"/uploads/{fileName}"; // Path relatif ke folder wwwroot
            }

            // Membuat target tabungan baru
            var target = new TargetTabungan
            {
                IdUser = userId,
                NamaTarget = dto.NamaTarget,
                Deskripsi = dto.Deskripsi,
                NominalTarget = dto.NominalTarget,
                NominalTerkumpul = 0,
                TanggalMulai = DateTime.UtcNow,
                TanggalTarget = dto.TanggalTarget,
                Status = "Sedang Menabung",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Gambar = imagePath // Menyimpan path gambar ke database
            };

            _context.TargetTabungans.Add(target);
            await _context.SaveChangesAsync();

            // Membuat response untuk mengembalikan data target yang telah dibuat
            var response = new TargetTabunganResponse
            {
                IdTargetTabungan = target.IdTargetTabungan,
                NamaTarget = target.NamaTarget,
                Deskripsi = target.Deskripsi,
                NominalTarget = target.NominalTarget,
                NominalTerkumpul = target.NominalTerkumpul,
                TanggalMulai = target.TanggalMulai,
                TanggalTarget = target.TanggalTarget,
                Status = target.Status,
                Gambar = target.Gambar // Menyertakan gambar dalam respons
            };

            return CreatedAtAction(nameof(GetTargetById), new { id = target.IdTargetTabungan }, response);
        }

        // GET: api/TargetTabungan
        [HttpGet]
        public async Task<IActionResult> GetTargets()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var targets = await _context.TargetTabungans
                .Where(t => t.IdUser == userId)
                .Select(t => new TargetTabunganResponse
                {
                    IdTargetTabungan = t.IdTargetTabungan,
                    NamaTarget = t.NamaTarget,
                    Deskripsi = t.Deskripsi,
                    NominalTarget = t.NominalTarget,
                    NominalTerkumpul = _context.Pengeluarans
                        .Where(p => p.IdTargetTabungan == t.IdTargetTabungan)
                        .Sum(p => (decimal?)p.Nominal) ?? 0,
                    TanggalMulai = t.TanggalMulai,
                    TanggalTarget = t.TanggalTarget,
                    Status = t.Status,
                    Gambar = t.Gambar // Menambahkan gambar dalam respons
                })
                .ToListAsync();

            return Ok(targets);
        }

        // GET: api/TargetTabungan/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTargetById(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var target = await _context.TargetTabungans
                .Where(t => t.IdTargetTabungan == id && t.IdUser == userId)
                .Select(t => new TargetTabunganResponse
                {
                    IdTargetTabungan = t.IdTargetTabungan,
                    NamaTarget = t.NamaTarget,
                    Deskripsi = t.Deskripsi,
                    NominalTarget = t.NominalTarget,
                    NominalTerkumpul = t.NominalTerkumpul,
                    TanggalMulai = t.TanggalMulai,
                    TanggalTarget = t.TanggalTarget,
                    Status = t.Status,
                    Gambar = t.Gambar // Menyertakan gambar dalam respons
                })
                .FirstOrDefaultAsync();

            if (target == null)
                return NotFound(new { message = "Target tabungan tidak ditemukan." });

            return Ok(target);
        }

        // DELETE: api/TargetTabungan/ResetTarget/5
        [HttpDelete("ResetTarget/{idTargetTabungan}")]
        public async Task<IActionResult> ResetTargetTabungan(int idTargetTabungan)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var target = await _context.TargetTabungans
                .FirstOrDefaultAsync(t => t.IdTargetTabungan == idTargetTabungan && t.IdUser == userId);

            if (target == null)
                return NotFound(new { message = "Target tabungan tidak ditemukan." });

            // Ambil semua pengeluaran terkait target ini
            var pengeluarans = await _context.Pengeluarans
                .Where(p => p.IdTargetTabungan == idTargetTabungan && p.IdUser == userId)
                .ToListAsync();

            if (pengeluarans.Any())
            {
                _context.Pengeluarans.RemoveRange(pengeluarans);
            }

            _context.TargetTabungans.Remove(target);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Target tabungan dan semua pengeluaran terkait berhasil dihapus." });
        }
        // ENDPOINT UNTUK MENGHAPUS TARGET SECARA OTOMATIS SETELAH 1 HARI DICAPAI
[HttpPost("AutoDeleteTarget")]
public async Task<IActionResult> AutoDeleteTarget()
{
    var now = DateTime.UtcNow;

    // Ambil semua target tabungan dengan status "Tercapai"
    var targets = await _context.TargetTabungans
        .Where(t => t.Status == "Tercapai" && t.TanggalTarget.HasValue)
        .ToListAsync(); // Ambil data terlebih dahulu

    // Filter target yang sudah lebih dari 1 hari setelah tanggal target
    var targetsToDelete = targets
        .Where(t => (now - t.TanggalTarget.Value).Days >= 1)
        .ToList();

    if (!targetsToDelete.Any())
    {
        return Ok(new { message = "Tidak ada target yang perlu dihapus." });
    }

    // Hapus target dan pengeluaran terkait
    foreach (var target in targetsToDelete)
    {
        // Hapus pengeluaran terkait target ini tanpa mengembalikan saldo
        var pengeluarans = await _context.Pengeluarans
            .Where(p => p.IdTargetTabungan == target.IdTargetTabungan)
            .ToListAsync();

        if (pengeluarans.Any())
        {
            _context.Pengeluarans.RemoveRange(pengeluarans);
        }

        // Hapus target tabungan
        _context.TargetTabungans.Remove(target);
    }

    await _context.SaveChangesAsync();

    return Ok(new { message = "Target yang telah tercapai lebih dari 1 hari telah dihapus." });
}


    }
}
