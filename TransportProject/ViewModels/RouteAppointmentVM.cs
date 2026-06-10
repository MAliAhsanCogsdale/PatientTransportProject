namespace TransportProject.ViewModels
{
    public class RouteAppointmentVM
    {
        //public int Id { get; set; }
        //public string DriverName { get; set; }
        //public string PatientName { get; set; }
        //public DateTime PickupTime { get; set; }
        //public string PickupAddress { get; set; }
        //public string HospitalName { get; set; }
        //public int? SequenceOrder { get; set; }
        //public string LOS { get; set; }
        //public decimal CPay { get; set; }
        //public int PCA { get; set; }
        //public int AESC { get; set; }
        //public int CESC { get; set; }
        //public int Seats { get; set; }
        //public decimal Miles { get; set; }
        //public string Notes { get; set; }
        public int Id { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime PickupTime { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public int? SequenceOrder { get; set; }
        public string? LOS { get; set; }
        public decimal? CPay { get; set; }
        public int? PCA { get; set; }
        public int? AESC { get; set; }
        public int? CESC { get; set; }
        public int? Seats { get; set; }
        public decimal? Miles { get; set; }
        public string? Notes { get; set; }
    }
}
