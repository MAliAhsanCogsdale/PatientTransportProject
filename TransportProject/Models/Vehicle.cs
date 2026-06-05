namespace TransportProject.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        public string VehicleName { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string VehicleNumber { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? Deleted { get; set; }
    }
}