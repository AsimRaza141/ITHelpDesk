namespace ITHelpDesk_Updated.Models
{
    public class ItemViewModel
    {
        public Item CurrentItem { get; set; }

        public List<Item> Items { get; set; }

        public Dictionary<string, List<string>> CustomItemNames { get; set; } = new Dictionary<string, List<string>>
    {
        { "Main Hardware", new List<string>() },
        { "Accessories", new List<string>() }
    };
        public Dictionary<string, List<string>> CustomCategories { get; set; } = new Dictionary<string, List<string>>();
        public List<string> CustomItemTypes { get; set; } = new List<string>();
        public List<string> CustomStorages { get; set; } = new List<string>();
    }
}
