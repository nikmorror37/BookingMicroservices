using Microsoft.AspNetCore.Identity;
using System;

namespace IdentityService.API.Domain.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string LastName  { get; set; } = null!;

        // Contact details
        public string? Address     { get; set; }
        public string? City        { get; set; }
        public string? State       { get; set; }
        public string? PostalCode  { get; set; }
        public string? Country     { get; set; }
        public DateTime? DateOfBirth       { get; set; }
    }
}