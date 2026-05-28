using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Identity;

namespace RainfallThree.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; } = false;
        public DateTime? LastLoginDate { get; set; }
        public string? Status { get; set; } = "Pending";
        public bool HasAcceptedTerms { get; set; }

        public string? AcceptedTermsVersion { get; set; }

        public DateTime? TermsAcceptedOn { get; set; }
    }
}
