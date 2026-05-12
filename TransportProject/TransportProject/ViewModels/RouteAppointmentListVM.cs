namespace TransportProject.ViewModels
{
    public class RouteAppointmentListVM
    {
        public List<RouteAppointmentVM> Items { get; set; } = new();

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }

        public string SearchDriver { get; set; }
        public string SearchPatient { get; set; }
        public DateTime? Date { get; set; }

        public bool SelectAll { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}
