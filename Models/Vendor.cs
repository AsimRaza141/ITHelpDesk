using System.ComponentModel.DataAnnotations;

namespace ITHelpDesk_Updated.Models
{
    public class Vendor
    {
        [Required(ErrorMessage = "Vendor Code is required.")]
        public string VendorCode { get; set; }
        [Required(ErrorMessage = "Vendor Name is required.")]
        public string VendorName { get; set; }
        [Required(ErrorMessage = "Vendor NTN is required.")]
        public string VendorNTN { get; set; }
        [Required(ErrorMessage = "Vendor CNIC is required.")]
        public string VendorCNIC { get; set; }
        [Required(ErrorMessage = "Vendor Contact No is required.")]
        public string VendorContactNo { get; set; }
        [Required(ErrorMessage = "Vendor Contact Person is required.")]
        public string VendorContactPerson { get; set; }
    }
}
