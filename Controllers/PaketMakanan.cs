using AuthAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TokoKebab.DTOs;
using TokoKebab.Models;

namespace TokoKebab.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaketMakananController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public PaketMakananController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/PaketMakanan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaketMakananDTOs>>> GetPaketMakanan()
        {
            var paketMakananList = await _context.PaketMakanans.ToListAsync();

            var result = paketMakananList.Select(p => new PaketMakananDTOs
            {
                Id_Paket = p.Id_Paket,
                Nama_Paket = p.Nama_Paket,
                Id_Kebab = p.Id_Kebab,
                Id_Snack = p.Id_Snack,
                Id_Drink = p.Id_Drink,
                Diskon = p.Diskon,
                Harga_Paket = p.Harga_Paket,
                Harga_Paket_After_Diskon = p.Harga_Paket_After_Diskon,
                Stok = p.Stok ?? 0,
                image = p.image  // Added image field
            }).ToList();

            return Ok(result);
        }

        // GET: api/PaketMakanan/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PaketMakananDTOs>> GetPaketMakanan(int id)
        {
            var paketMakanan = await _context.PaketMakanans.FirstOrDefaultAsync(p => p.Id_Paket == id);

            if (paketMakanan == null)
            {
                return NotFound();
            }

            var paketDTO = new PaketMakananDTOs
            {
                Id_Paket = paketMakanan.Id_Paket,
                Nama_Paket = paketMakanan.Nama_Paket,
                Id_Kebab = paketMakanan.Id_Kebab,
                Id_Snack = paketMakanan.Id_Snack,
                Id_Drink = paketMakanan.Id_Drink,
                Diskon = paketMakanan.Diskon,
                Harga_Paket = paketMakanan.Harga_Paket,
                Harga_Paket_After_Diskon = paketMakanan.Harga_Paket_After_Diskon,
                Stok = paketMakanan.Stok ?? 0,
                image = paketMakanan.image  // Added image field
            };

            return Ok(paketDTO);
        }

        // POST: api/PaketMakanan
        [HttpPost]
        public async Task<ActionResult<PaketMakananDTOs>> PostPaketMakanan(PaketMakananCreateDto paketMakananDTO)
        {
            var paketMakanan = new PaketMakanan
            {
                Nama_Paket = paketMakananDTO.Nama_Paket,
                Id_Kebab = paketMakananDTO.Id_Kebab,
                Id_Snack = paketMakananDTO.Id_Snack,
                Id_Drink = paketMakananDTO.Id_Drink,
                Diskon = paketMakananDTO.Diskon,
                Harga_Paket = paketMakananDTO.Harga_Paket,
                Harga_Paket_After_Diskon = paketMakananDTO.Harga_Paket_After_Diskon,
                Stok = paketMakananDTO.Stok,
                image = paketMakananDTO.image  // Handle image field
            };

            _context.PaketMakanans.Add(paketMakanan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaketMakanan), new { id = paketMakanan.Id_Paket }, new PaketMakananDTOs
            {
                Id_Paket = paketMakanan.Id_Paket,
                Nama_Paket = paketMakanan.Nama_Paket,
                Id_Kebab = paketMakanan.Id_Kebab,
                Id_Snack = paketMakanan.Id_Snack,
                Id_Drink = paketMakanan.Id_Drink,
                Diskon = paketMakanan.Diskon,
                Harga_Paket = paketMakanan.Harga_Paket,
                Harga_Paket_After_Diskon = paketMakanan.Harga_Paket_After_Diskon,
                Stok = paketMakanan.Stok ?? 0,
                image = paketMakanan.image  // Return image field
            });
        }

        // PUT: api/PaketMakanan/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaketMakanan(int id, PaketMakananUpdateDto paketMakananDTO)
        {
            var existingPaket = await _context.PaketMakanans.FindAsync(id);
            if (existingPaket == null)
            {
                return NotFound();
            }

            existingPaket.Nama_Paket = paketMakananDTO.Nama_Paket;
            existingPaket.Id_Kebab = paketMakananDTO.Id_Kebab;
            existingPaket.Id_Snack = paketMakananDTO.Id_Snack;
            existingPaket.Id_Drink = paketMakananDTO.Id_Drink;
            existingPaket.Diskon = paketMakananDTO.Diskonn;
            existingPaket.Harga_Paket = paketMakananDTO.Harga_Paket;
            existingPaket.Harga_Paket_After_Diskon = paketMakananDTO.Harga_Paket_After_Diskon;
            existingPaket.Stok = paketMakananDTO.Stok;
            existingPaket.image = paketMakananDTO.image;  // Handle image field

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/PaketMakanan/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaketMakanan(int id)
        {
            var paketMakanan = await _context.PaketMakanans.FindAsync(id);
            if (paketMakanan == null)
            {
                return NotFound();
            }

            _context.PaketMakanans.Remove(paketMakanan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
