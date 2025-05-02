namespace ITHelpDesk_Updated.Models
{
    public class Achievement
    {
        public int AchievementId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string USR_NAME { get; set; }
        public string USR_FULL_NAME { get; set; }
        public DateTime? DateAchieved { get; set; }
    }
}
