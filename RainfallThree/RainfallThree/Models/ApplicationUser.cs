using Microsoft.AspNetCore.Identity;

namespace RainfallThree.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; } = false;
    }
}
