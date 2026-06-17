using System.ComponentModel.DataAnnotations;

namespace TransportProject.Models
{
    /// <summary>
    /// HIPAA §164.312(b) Audit Controls: records every access to or export of PHI.
    /// Retain per your retention policy (HIPAA generally implies 6 years for related documentation).
    /// </summary>
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(256)]
        public string UserName { get; set; } = "Anonymous";

        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>e.g. "ViewAppointments", "ExportPHI", "ImportPHI".</summary>
        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>The PHI entity affected, e.g. "Appointment", "Patient".</summary>
        [MaxLength(100)]
        public string? EntityType { get; set; }

        /// <summary>Identifier(s) of affected records. Avoid storing PHI itself here.</summary>
        [MaxLength(500)]
        public string? EntityIds { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; } = true;

        [MaxLength(500)]
        public string? Details { get; set; }
    }
}