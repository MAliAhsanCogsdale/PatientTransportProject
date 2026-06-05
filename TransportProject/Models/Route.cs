namespace TransportProject.Models
{
    public class Route
    {
        public int Id { get; set; }

        public string RouteDescription { get; set; } = string.Empty;

        public int DriverId { get; set; }

        public DateTime RouteDate { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime? Deleted { get; set; }
        public Driver? Driver { get; set; }
    }
}