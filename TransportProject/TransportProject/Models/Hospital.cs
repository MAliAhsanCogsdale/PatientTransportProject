using System.ComponentModel.DataAnnotations;

namespace TransportProject.Models
{
    public class Hospital
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; }

        public string? Phone { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? HospitalType { get; set; }

        public bool EmergencyAvailable { get; set; }

        public string? OperatingHours { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? Deleted { get; set; }
    }
}