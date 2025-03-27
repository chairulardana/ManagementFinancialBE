using Microsoft.AspNetCore.Mvc;
using TokoKebab.Models;
using TokoKebab.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthAPI.Data;
using TokoKebab.DTO;

namespace TokoKebab.Controllers
{
    [Route("api/Kebab")]
    [ApiController]
    public class KebabController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public KebabController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/Kebab
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KebabDTO>>> GetKebabs()
        {
            var kebabs = await _context.Kebabs
                .Select(k => new KebabDTO
                {
                    Id_Kebab = k.Id_Kebab,
                    Nama_Kebab = k.Nama_Kebab,
                    Harga = k.Harga,
                    Size = k.Size,
                    Level = k.Level,
                    Stock = k.Stock,
                    ImageUrl = k.Image  // Menyertakan URL gambar yang disimpan di database
                })
                .ToListAsync();

            return Ok(kebabs);
        }

        // POST: api/Kebab
        [HttpPost]
        public async Task<ActionResult<KebabDTO>> PostKebab([FromBody] KebabCreateDTO kebabDTO)
        {
            var kebab = new Kebab
            {
                Nama_Kebab = kebabDTO.Nama_Kebab,
                Harga = kebabDTO.Harga,
                Size = kebabDTO.Size,
                Level = kebabDTO.Level,
                Stock = kebabDTO.Stock
            };

            // Menentukan folder untuk menyimpan gambar
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Membuat folder jika belum ada
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Menangani unggah gambar jika ada (image URL provided in the JSON request)
            if (!string.IsNullOrEmpty(kebabDTO.ImageUrl))
            {
                string imagePath = kebabDTO.ImageUrl;
                kebab.Image = imagePath; // Save the image path or URL as is
            }

            _context.Kebabs.Add(kebab);
            await _context.SaveChangesAsync();

            // Mengembalikan URL gambar yang baru dimasukkan dalam data response
            kebabDTO.ImageUrl = kebab.Image;

            return CreatedAtAction(nameof(GetKebabs), new { id_Kebab = kebab.Id_Kebab }, kebabDTO);
        }

        // PUT: api/Kebab/{id_Kebab}
        [HttpPut("{id_Kebab}")]
        public async Task<IActionResult> PutKebab(int id_Kebab, [FromBody] KebabDTO kebabDTO)
        {
            if (id_Kebab != kebabDTO.Id_Kebab)
            {
                return BadRequest("ID tidak sesuai.");
            }

            var kebab = await _context.Kebabs.FindAsync(id_Kebab);
            if (kebab == null)
            {
                return NotFound("Kebab tidak ditemukan.");
            }

            kebab.Nama_Kebab = kebabDTO.Nama_Kebab;
            kebab.Harga = kebabDTO.Harga;
            kebab.Size = kebabDTO.Size;
            kebab.Level = kebabDTO.Level;
            kebab.Stock = kebabDTO.Stock;

            // Menangani update gambar jika ada
            if (!string.IsNullOrEmpty(kebabDTO.ImageUrl))
            {
                kebab.Image = kebabDTO.ImageUrl;  // Updating the image URL path
            }

            _context.Entry(kebab).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Kebab/{id_Kebab}
       [HttpDelete("{id_Kebab}")]
public async Task<IActionResult> DeleteKebab(int id_Kebab)
{
    var kebab = await _context.Kebabs.FindAsync(id_Kebab);
    if (kebab == null)
    {
        return NotFound("Kebab tidak ditemukan.");
    }

    // Set the foreign key references to null in DetailTransaksi
    var detailTransaksiRecords = _context.DetailTransaksi.Where(dt => dt.Id_Kebab == id_Kebab).ToList();
    foreach (var record in detailTransaksiRecords)
    {
        record.Id_Kebab = null;  // Set the foreign key to null
    }

    // Save the changes before deleting the Kebab
    await _context.SaveChangesAsync();

    // Now delete the Kebab
    _context.Kebabs.Remove(kebab);
    await _context.SaveChangesAsync();

    return NoContent();
}

    }
}
