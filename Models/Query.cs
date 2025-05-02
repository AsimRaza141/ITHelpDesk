namespace ITHelpDesk_Updated.Models
{
    public class Query
    {
        public int QueryId { get; set; }
        public string TicketNumber { get; set; }
        public string USR_NAME { get; set; }
        public string USR_FULL_NAME { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string AssignedTo { get; set; }
    }
}
