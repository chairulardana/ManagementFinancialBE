namespace TokoKebab.DTO
{
    public class KebabCreateDTO
{
        public string Nama_Kebab { get; set; }
        public decimal Harga { get; set; }
        public string? Size { get; set; }
        public int Level { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }  
    }

}
