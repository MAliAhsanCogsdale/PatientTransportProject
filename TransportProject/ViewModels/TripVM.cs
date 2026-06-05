namespace TransportProject.ViewModels
{
    public class TripVm
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string PatientPhone { get; set; } = "";
        public int PatientAge { get; set; } = 0;
        public string PickupAddress { get; set; } = "";
        public string HospitalAddress { get; set; } = "";
        public string HospitalName { get; set; } = "";
        public string HospitalPhone { get; set; } = "";
        public DateTime? PickupTime { get; set; }
        public DateTime DropTime { get; set; }
        public string RideId { get; set; } = "";
        public string? Notes { get; set; } = "";

        public string LOS { get; set; } = "";
        public decimal CPay { get; set; }

        public bool PCA { get; set; }
        public bool AESC { get; set; }
        public bool CESC { get; set; }

        public int Seats { get; set; }
        public decimal Miles { get; set; }
    }

    public class RawAppointmentDto
    {
        public string PatientName { get; set; } = "";
        public string PickupAddress { get; set; } = "";
        public string HospitalName { get; set; } = "";
        public DateTime AppointmentTime { get; set; }
        public DateTime PickupTime { get; set; }
        public string Notes { get; set; } = "";

        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }
    }
}