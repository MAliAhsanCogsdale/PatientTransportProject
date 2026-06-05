namespace TransportProject.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime? Deleted { get; set; }
    }
}
