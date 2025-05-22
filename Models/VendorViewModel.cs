namespace ITHelpDesk_Updated.Models
{
    public class VendorViewModel
    {
        public Vendor CurrentVendor { get; set; }
        public List<Vendor> Vendors { get; set; }
        public bool IsEditMode { get; set; }
    }
}
