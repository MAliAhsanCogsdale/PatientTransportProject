namespace TransportProject.Models
{
    public class RouteAppointment
    {
        public int Id { get; set; }

        public int RouteId { get; set; }

        public int AppointmentId { get; set; }

        public int SequenceOrder { get; set; }
        public string? LOS { get; set; }

        public decimal? CPay { get; set; }

        public int? PCA { get; set; }

        public int? AESC { get; set; }

        public int? CESC { get; set; }

        public int? Seats { get; set; }

        public decimal? Miles { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? Deleted { get; set; }
        public Route Route { get; internal set; }
        public Appointment Appointment { get; internal set; }
    }
}