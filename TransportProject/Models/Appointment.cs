namespace TransportProject.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public int HospitalId { get; set; }

        public DateTime AppointmentTime { get; set; }

        public DateTime PickupTime { get; set; }

        public string Status { get; set; } = "Scheduled";

        public string PickupAddress { get; set; } = string.Empty;

        public double PickupLatitude { get; set; }

        public double PickupLongitude { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? Deleted { get; set; }
        public Patient? Patient { get; set; }
        public Hospital? Hospital { get; set; }
    }
}