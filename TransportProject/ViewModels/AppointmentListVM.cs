namespace TransportProject.ViewModels
{
    public class AppointmentListVM
    {
        public List<AppointmentItemVM> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
    }

    public class AppointmentItemVM
    {
        public int Id { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime PickupTime { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public DateTime? DropOffTime { get; set; }
        public string DropOffAddress { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }
        public string? LOS { get; set; }
        public decimal? CPay { get; set; }
        public decimal? Miles { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
