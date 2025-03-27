using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokoKebab.Models;
using TokoKebab.DTOs;
using AuthAPI.Data;

namespace TokoKebab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetailTransaksiController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public DetailTransaksiController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var transactions = _context.DetailTransaksi
                .Include(dt => dt.Drink)
                .Include(dt => dt.Kebab)
                .Include(dt => dt.Snack)
                .Include(dt => dt.PaketMakanan)
                .Include(dt => dt.User)
                .Select(dt => new GetDetailTransaksiDto
                {
                    Id_DetailTransaksi = dt.Id_DetailTransaksi,
                    // Only include the product details if they exist
                    Nama_Drink = dt.Drink != null ? dt.Drink.Nama_Minuman : null,
                    Nama_Kebab = dt.Kebab != null ? dt.Kebab.Nama_Kebab : null,
                    Nama_Snack = dt.Snack != null ? dt.Snack.Nama_Snack : null,
                    Nama_Paket = dt.PaketMakanan != null ? dt.PaketMakanan.Nama_Paket : null,

                    TanggalTransaksi = dt.TanggalTransaksi,
                    Jumlah = dt.Jumlah,
                    TotalHarga = dt.TotalHarga,
                    Id_User = dt.Id_User,
                    Name = dt.User != null ? dt.User.Name : null,
                })
                // Filter to only include products that are not null (those that were purchased)
                .Where(dt => dt.Nama_Drink != null || dt.Nama_Kebab != null || dt.Nama_Snack != null || dt.Nama_Paket != null)
                .ToList();

            return Ok(transactions);
        }

        [HttpPost]
        public IActionResult Create([FromBody] PostDetailTransaksiDto dto)
        {
            // Authentication check
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not logged in.");

            // Validate exactly one item is selected
            var selectedItems = new[] { dto.Id_Drink, dto.Id_Kebab, dto.Id_Snack, dto.Id_Paket }
                .Count(id => id.HasValue);

            if (selectedItems != 1)
                return BadRequest("Pilih tepat satu item (Minuman, Kebab, Snack, atau Paket).");

            // Get item price, name and check stock
            var (hargaItem, namaItem, stockItem) = GetItemDetails(dto);
            if (hargaItem == 0)
                return BadRequest("Item tidak ditemukan.");

            // Check stock availability
            if (stockItem < dto.Jumlah)
            {
                return BadRequest($"Stok {namaItem} tidak mencukupi. Stok tersedia: {stockItem}");
            }

            // Start transaction
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Create transaction record
                var transaksi = new DetailTransaksi
                {
                    Id_User = int.Parse(userId),
                    Id_Drink = dto.Id_Drink,
                    Id_Kebab = dto.Id_Kebab,
                    Id_Snack = dto.Id_Snack,
                    Id_Paket = dto.Id_Paket,
                    TanggalTransaksi = DateTime.Now,
                    Jumlah = dto.Jumlah,
                    TotalHarga = hargaItem * dto.Jumlah
                };

                _context.DetailTransaksi.Add(transaksi);

                // Reduce stock
                if (dto.Id_Drink.HasValue)
                {
                    var drink = _context.Drinks.Find(dto.Id_Drink);
                    if (drink != null) drink.Stock -= dto.Jumlah;
                }
                else if (dto.Id_Kebab.HasValue)
                {
                    var kebab = _context.Kebabs.Find(dto.Id_Kebab);
                    if (kebab != null) kebab.Stock -= dto.Jumlah;
                }
                else if (dto.Id_Snack.HasValue)
                {
                    var snack = _context.Snacks.Find(dto.Id_Snack);
                    if (snack != null) snack.Stock -= dto.Jumlah;
                }
                else if (dto.Id_Paket.HasValue)
                {
                    var paket = _context.PaketMakanans.Find(dto.Id_Paket);
                    if (paket != null) paket.Stok -= dto.Jumlah;
                }

                _context.SaveChanges();
                transaction.Commit();

                // Return formatted response
                return Ok(new
                {
                    transaksi.Id_DetailTransaksi,
                    Item = namaItem,
                    HargaSatuan = hargaItem,
                    transaksi.Jumlah,
                    transaksi.TotalHarga,
                    transaksi.TanggalTransaksi,
                    Message = "Pembelian berhasil. Stok telah diperbarui."
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] EditDetailTransaksiDto dto)
        {
            var existing = _context.DetailTransaksi.Find(id);
            if (existing == null)
                return NotFound("Detail transaksi tidak ditemukan.");

            // Store original values for stock adjustment
            var originalItemType = existing.Id_Drink.HasValue ? "Drink" :
                                 existing.Id_Kebab.HasValue ? "Kebab" :
                                 existing.Id_Snack.HasValue ? "Snack" : "Paket";
            var originalItemId = existing.Id_Drink ?? existing.Id_Kebab ?? existing.Id_Snack ?? existing.Id_Paket;
            var originalQuantity = existing.Jumlah;

            // Start transaction
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Update only provided fields
                if (dto.Id_Drink.HasValue) existing.Id_Drink = dto.Id_Drink;
                if (dto.Id_Kebab.HasValue) existing.Id_Kebab = dto.Id_Kebab;
                if (dto.Id_Snack.HasValue) existing.Id_Snack = dto.Id_Snack;
                if (dto.Id_Paket.HasValue) existing.Id_Paket = dto.Id_Paket;
                if (dto.Jumlah.HasValue) existing.Jumlah = dto.Jumlah.Value;
                if (dto.TanggalTransaksi.HasValue) existing.TanggalTransaksi = dto.TanggalTransaksi.Value;

                // Recalculate total if any price-related fields changed
                if (dto.Jumlah.HasValue || dto.Id_Drink.HasValue || dto.Id_Kebab.HasValue || 
                    dto.Id_Snack.HasValue || dto.Id_Paket.HasValue)
                {
                    var (hargaItem, _, stockItem) = GetItemDetails(new PostDetailTransaksiDto
                    {
                        Id_Drink = existing.Id_Drink,
                        Id_Kebab = existing.Id_Kebab,
                        Id_Snack = existing.Id_Snack,
                        Id_Paket = existing.Id_Paket
                    });

                    // Check stock for new quantity
                    if (dto.Jumlah.HasValue && stockItem < dto.Jumlah.Value)
                    {
                        return BadRequest($"Stok tidak mencukupi. Stok tersedia: {stockItem}");
                    }

                    existing.TotalHarga = hargaItem * existing.Jumlah;
                }

                // Adjust stock for the original item
                if (originalItemId.HasValue && dto.Jumlah.HasValue)
                {
                    var quantityDifference = dto.Jumlah.Value - originalQuantity;
                    AdjustStock(originalItemType, originalItemId.Value, quantityDifference);
                }

                // Adjust stock for new item if changed
                if ((dto.Id_Drink.HasValue || dto.Id_Kebab.HasValue || dto.Id_Snack.HasValue || dto.Id_Paket.HasValue) && 
                    dto.Jumlah.HasValue)
                {
                    var newItemType = dto.Id_Drink.HasValue ? "Drink" :
                                    dto.Id_Kebab.HasValue ? "Kebab" :
                                    dto.Id_Snack.HasValue ? "Snack" : "Paket";
                    var newItemId = dto.Id_Drink ?? dto.Id_Kebab ?? dto.Id_Snack ?? dto.Id_Paket;
                    
                    // Check if item type changed
                    if (newItemType != originalItemType || newItemId != originalItemId)
                    {
                        // Return stock to original item
                        AdjustStock(originalItemType, originalItemId.Value, originalQuantity);
                        
                        // Reduce stock from new item
                        AdjustStock(newItemType, newItemId.Value, -dto.Jumlah.Value);
                    }
                }

                _context.SaveChanges();
                transaction.Commit();

                return Ok(existing);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var transaksi = _context.DetailTransaksi.Find(id);
            if (transaksi == null)
                return NotFound("Detail transaksi tidak ditemukan.");

            // Start transaction
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Return stock before deleting
                if (transaksi.Id_Drink.HasValue)
                {
                    var drink = _context.Drinks.Find(transaksi.Id_Drink);
                    drink.Stock += transaksi.Jumlah;
                }
                else if (transaksi.Id_Kebab.HasValue)
                {
                    var kebab = _context.Kebabs.Find(transaksi.Id_Kebab);
                    kebab.Stock += transaksi.Jumlah;
                }
                else if (transaksi.Id_Snack.HasValue)
                {
                    var snack = _context.Snacks.Find(transaksi.Id_Snack);
                    snack.Stock += transaksi.Jumlah;
                }
                else if (transaksi.Id_Paket.HasValue)
                {
                    var paket = _context.PaketMakanans.Find(transaksi.Id_Paket);
                    paket.Stok += transaksi.Jumlah;
                }

                _context.DetailTransaksi.Remove(transaksi);
                _context.SaveChanges();
                transaction.Commit();

                return NoContent();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        private (decimal harga, string nama, int stok) GetItemDetails(PostDetailTransaksiDto dto)
        {
            if (dto.Id_Drink.HasValue)
            {
                var item = _context.Drinks.Find(dto.Id_Drink);
                if (item != null)
                {
                    return (item.Harga, item.Nama_Minuman, item.Stock);
                }
            }
            if (dto.Id_Kebab.HasValue)
            {
                var item = _context.Kebabs.Find(dto.Id_Kebab);
                if (item != null)
                {
                    return (item.Harga, item.Nama_Kebab, item.Stock);
                }
            }
            if (dto.Id_Snack.HasValue)
            {
                var item = _context.Snacks.Find(dto.Id_Snack);
                if (item != null)
                {
                    return (item.Harga, item.Nama_Snack, item.Stock);
                }
            }
            if (dto.Id_Paket.HasValue)
            {
                var item = _context.PaketMakanans.Find(dto.Id_Paket);
                if (item != null)
                {
                    return (item.Harga_Paket_After_Diskon, item.Nama_Paket, item.Stok ?? 0);
                }
            }

            return (0, "No Item", 0);
        }

        private void AdjustStock(string itemType, int itemId, int quantity)
        {
            switch (itemType)
            {
                case "Drink":
                    var drink = _context.Drinks.Find(itemId);
                    if (drink != null) drink.Stock += quantity;
                    break;
                case "Kebab":
                    var kebab = _context.Kebabs.Find(itemId);
                    if (kebab != null) kebab.Stock += quantity;
                    break;
                case "Snack":
                    var snack = _context.Snacks.Find(itemId);
                    if (snack != null) snack.Stock += quantity;
                    break;
                case "Paket":
                    var paket = _context.PaketMakanans.Find(itemId);
                    if (paket != null) paket.Stok += quantity;
                    break;
            }
        }
    }
}
