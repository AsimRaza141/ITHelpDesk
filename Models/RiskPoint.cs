namespace ITHelpDesk_Updated.Models
{
    public class RiskPoint
    {
        public int RiskPointId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string USR_NAME { get; set; }
        public string USR_FULL_NAME { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
