namespace CatalogService.API.Domain.Models
{
    public class Hotel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
        public int Stars { get; set; }
        public double DistanceFromCenter { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
