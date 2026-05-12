namespace TransportProject.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string? Address1 { get; set; }

        public string? Address2 { get; set; }

        public TimeSpan ShiftStartTime { get; set; }

        public TimeSpan ShiftEndTime { get; set; }

        public string? Holiday { get; set; }

        public int VehicleId { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime? Deleted { get; set; }

        // current driver location and last drop time - nullable
        public double? CurrentLat { get; set; }
        public double? CurrentLng { get; set; }
        public DateTime? LastDropTime { get; set; }

    }
}