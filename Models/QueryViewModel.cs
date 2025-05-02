namespace ITHelpDesk_Updated.Models
{
    public class QueryViewModel
    {
        public List<Query> Queries { get; set; }
        public Query CurrentQuery { get; set; }
        public string UserName { get; set; }
        public List<string> AssignedUsers { get; set; }

    }
}
