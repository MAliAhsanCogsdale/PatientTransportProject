namespace TransportProject.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public int HospitalId { get; set; }

        public int? DriverId { get; set; } // New: Link to driver
        public DateTime AppointmentTime { get; set; }

        public DateTime PickupTime { get; set; }

        public string Status { get; set; } = "Scheduled";

        // Pickup Information
        public string PickupAddress { get; set; } = string.Empty;
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        // Drop-off Information (NEW)
        public DateTime? DropOffTime { get; set; }
        public string? DropOffAddress { get; set; }
        public double? DropOffLatitude { get; set; }
        public double? DropOffLongitude { get; set; }


        // Transport Details (Moved from RouteAppointment)
        public string? LOS { get; set; } // Length of Stay: e.g., "W T T S"
        public decimal? CPay { get; set; }
        public int? PCA { get; set; }
        public int? AESC { get; set; }
        public int? CESC { get; set; }
        public int? Seats { get; set; }
        public decimal? Miles { get; set; }
        public string? Notes { get; set; }
        public int? SequenceOrder { get; set; } // New: For route ordering
        public bool IsActive { get; set; } = true;
        public DateTime? Deleted { get; set; }
        // Navigation Properties
        public Patient? Patient { get; set; }
        public Hospital? Hospital { get; set; }
        public Driver? Driver { get; set; }
    }
}