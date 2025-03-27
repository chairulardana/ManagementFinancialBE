using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AuthAPI.Data;
using TokoKebab.DTO;

namespace TokoKebab.Controllers
{
   [Route("api/Snack")]
    [ApiController]
    public class SnackController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public SnackController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Snack>>> GetSnack()
        {
            return await _context.Snacks.ToListAsync();
        }

        [HttpGet("{id_Snack}")]
        public async Task<ActionResult<Snack>> GetSnackById(int id_Snack)
        {
            var snack = await _context.Snacks.FindAsync(id_Snack);
            if (snack == null)
                return NotFound();

            return snack;
        }

        [HttpPost]
        public async Task<ActionResult<Snack>> PostSnack(SnackCreateDTOs snackDto)
        {
            var snack = new Snack
            {
                Nama_Snack = snackDto.NamaSnack,
                Harga = snackDto.Harga,
                Stock = snackDto.Stock,
                Image = snackDto.Image
            };

            _context.Snacks.Add(snack);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSnackById), new { id_Snack = snack.Id_Snack }, snack);
        }

        [HttpPut("{id_Snack}")]
        public async Task<IActionResult> PutSnack(int id_Snack, SnackUpdateDTO snackDto)
        {
            var snack = await _context.Snacks.FindAsync(id_Snack);
            if (snack == null)
                return NotFound();

            snack.Nama_Snack = snackDto.NamaSnack;
            snack.Harga = snackDto.Harga;
            snack.Stock = snackDto.Stock;
            snack.Image = snackDto.Image;

            _context.Entry(snack).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id_Snack}")]
        public async Task<IActionResult> DeleteSnack(int id_Snack)
        {
            var snack = await _context.Snacks.FindAsync(id_Snack);
            if (snack == null)
                return NotFound();

            _context.Snacks.Remove(snack);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
