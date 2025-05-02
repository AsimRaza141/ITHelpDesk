namespace ITHelpDesk_Updated.Models
{
    public class AchievementViewModel
    {
        public List<Achievement> Achievements { get; set; }
        public Achievement CurrentAchievement { get; set; }
        public string USR_NAME { get; set; }
        public string USR_FULL_NAME { get; set; }
    }
}
