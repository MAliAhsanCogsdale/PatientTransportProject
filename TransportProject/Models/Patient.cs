namespace TransportProject.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
        public bool IsWheelchairRequired { get; set; }
        public int HospitalId { get; set; }                  // Foreign key to Hospital
        public DateTime VisitTime { get; set; }             // Appointment / Visit time
        public string? VisitType { get; set; }              // e.g., Checkup, Emergency, Surgery
        public string? Notes { get; set; }                  // Optional notes about patient
        public bool IsActive { get; set; } = true;
        public DateTime? Deleted { get; set; }

        // Optional navigation property if using EF Core
        public Hospital? Hospital { get; set; }
    }
}
