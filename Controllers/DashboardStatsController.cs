using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokoKebab.Dashboard;
using AuthAPI.Data;
using System.Linq;

namespace TokoKebab.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardStatsController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public DashboardStatsController(AuthDbContext context)
        {
            _context = context;
        }

        // Endpoint: Mendapatkan statistik harian berdasarkan tanggal
        [HttpGet("{date}")]
        public IActionResult GetDashboardStats(string date)
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                return BadRequest("Invalid date format. Please use 'YYYY-MM-DD'.");
            }

            // Ambil data dari DashboardStats dengan relasi ke TopSellingProduct
            var stats = _context.DashboardStats
                .Include(ds => ds.TopSellingProduct)
                .FirstOrDefault(ds => ds.Tanggal == targetDate);

            if (stats == null)
            {
                return NotFound($"No data found for the date: {date}");
            }

            // Kembalikan data dalam format JSON
            return Ok(new
            {
                Tanggal = stats.Tanggal.ToString("yyyy-MM-dd"),
                TotalTransactions = stats.TotalTransactions,
                TotalPemasukan = stats.TotalPemasukan,
                TotalCustomers = stats.TotalCustomers,
                TopSellingProduct = stats.TopSellingProduct != null ? new
                {
                    stats.TopSellingProduct.ProductName,
                    stats.TopSellingProduct.UnitsSold,
                    stats.TopSellingProduct.Revenue
                } : null
            });
        }

        // Endpoint: Mendapatkan statistik mingguan
        [HttpGet("weekly")]
        public IActionResult GetWeeklyStats()
        {
            var startDate = DateTime.Today.AddDays(-7);

            // Ambil data mingguan dari DashboardStats
            var weeklyStats = _context.DashboardStats
                .Include(ds => ds.TopSellingProduct)
                .Where(ds => ds.Tanggal >= startDate)
                .OrderBy(ds => ds.Tanggal)
                .ToList();

            if (!weeklyStats.Any())
            {
                return NotFound("No data available for the past week.");
            }

            // Kembalikan data dalam format JSON
            return Ok(weeklyStats.Select(stats => new
            {
                Tanggal = stats.Tanggal.ToString("yyyy-MM-dd"),
                TotalTransactions = stats.TotalTransactions,
                TotalPemasukan = stats.TotalPemasukan,
                TotalCustomers = stats.TotalCustomers,
                TopSellingProduct = stats.TopSellingProduct != null ? new
                {
                    stats.TopSellingProduct.ProductName,
                    stats.TopSellingProduct.UnitsSold,
                    stats.TopSellingProduct.Revenue
                } : null
            }));
        }

        // Endpoint: Mendapatkan penjualan per jam berdasarkan tanggal
        [HttpGet("sales/hourly/{date}")]
        public IActionResult GetSalesPerHour(string date)
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                return BadRequest("Invalid date format. Please use 'YYYY-MM-DD'.");
            }

            // Ambil data penjualan per jam dari SalesPerHour
            var salesData = _context.SalesPerHour
                .Where(s => s.Tanggal == targetDate)
                .OrderBy(s => s.Hour)
                .ToList();

            if (!salesData.Any())
            {
                return NotFound($"No hourly sales data found for the date: {date}");
            }

            // Kembalikan data dalam format JSON
            return Ok(salesData.Select(s => new
            {
                Hour = s.Hour,
                Sales = s.Sales
            }));
        }
    }
}
