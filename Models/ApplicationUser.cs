using Microsoft.AspNetCore.Identity;

namespace MosqueRegistrationApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string IcNumber { get; set; }
        public string CurrentAddress { get; set; }
        public string MaritalStatus { get; set; }
        public int ResidencyDurationYears { get; set; }
        public string? ProofOfResidencyImagePath { get; set; }
    }
}