using System;
using System.ComponentModel.DataAnnotations;

namespace BookingWebApp.Models;

public class EditProfileVm
{
    [Required]
    [Display(Name="First name")]
    public string? FirstName { get; set; }

    [Required]
    [Display(Name="Last name")]
    public string? LastName  { get; set; }

    [Display(Name="Address")]
    public string? Address   { get; set; }

    [Display(Name="City")]
    public string? City      { get; set; }

    [Display(Name="State")]
    public string? State     { get; set; }

    [Display(Name="Postal code")]
    public string? PostalCode { get; set; }

    [Display(Name="Country")]
    public string? Country   { get; set; }

    [Display(Name="Date of birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
} 