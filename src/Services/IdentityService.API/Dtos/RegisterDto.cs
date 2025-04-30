using System;

namespace IdentityService.API.Dtos
{
    public class RegisterDto
    {
        public string   Email        { get; set; } = null!;
        public string   Password     { get; set; } = null!;
        public string   FirstName    { get; set; } = null!;
        public string   LastName     { get; set; } = null!;
        public string?  Address      { get; set; }
        public string?  City         { get; set; }
        public string?  State        { get; set; }
        public string?  PostalCode   { get; set; }
        public string?  Country      { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
