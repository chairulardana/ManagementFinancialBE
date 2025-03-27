using System;
using System.Collections.Generic;

namespace TokoKebab.Dashboard.DTO
{
public class DashboardStatsDTO
{
    public string Tanggal { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public decimal TotalPemasukan { get; set; }
    public TopSellingProductDTO? TopSellingProduct { get; set; } 
    public int TotalCustomers { get; set; }
    public List<SalesPerHourDTO>? SalesPerDay { get; set; }
}

public class TopSellingProductDTO
{
    public string? ProductName { get; set; }
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesPerHourDTO
{
    public int Hour { get; set; }
    public decimal Sales { get; set; }
}
}