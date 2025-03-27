using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AuthAPI.Models;
using AuthAPI.Data;
using TokoKebab.Models.DTO;
using AuthAPI.Data; // Tambahkan DTO

namespace TokoKebab.Controllers
{
    [Route("api/Drink")]
    [ApiController]
    public class DrinkController : ControllerBase
    {
        private readonly AuthDbContext _context;
        public DrinkController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/Drink
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Drink>>> GetDrink()
        {
            return await _context.Drinks.ToListAsync();
        }

        // POST: api/Drink
        [HttpPost]
        public async Task<ActionResult<Drink>> PostDrink(DrinkCreateDTO drinkDto)
        {
            var drink = new Drink
            {
                Nama_Minuman = drinkDto.NamaMinuman,
                Harga = drinkDto.Harga,
                Suhu = drinkDto.Suhu,
                Stock = drinkDto.Stock,
                Image = drinkDto.Image
            };

            _context.Drinks.Add(drink);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDrink), new { id = drink.Id_Drink }, drink);
        }

        // PUT: api/Drink/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDrink(int id, DrinkUpdateDTO drinkDto)
        {
            var drink = await _context.Drinks.FindAsync(id);
            if (drink == null)
                return NotFound();

            drink.Nama_Minuman = drinkDto.NamaMinuman;
            drink.Harga = drinkDto.Harga;
            drink.Suhu = drinkDto.Suhu;
            drink.Stock = drinkDto.Stock;
            drink.Image = drinkDto.Image;
            

            _context.Entry(drink).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Drink/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDrink(int id)
        {
            var drink = await _context.Drinks.FindAsync(id);
            if (drink == null)
                return NotFound();

            _context.Drinks.Remove(drink);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
