namespace BookingWebApp.Models;

using System.ComponentModel.DataAnnotations;

public class CustomerInfoVm
{
    [Required, StringLength(55, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-zĄąĆćĘęŁłŃńÓóŚśŹźŻż'\- ]{2,55}$",
        ErrorMessage = "2-55 letters; Latin/Polish, space or '-' allowed.")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(55, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-zĄąĆćĘęŁłŃńÓóŚśŹźŻż'\- ]{2,55}$",
        ErrorMessage = "2-55 letters; Latin/Polish, space or '-' allowed.")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,}$",
        ErrorMessage = "Enter a valid e-mail (user@mail.com).")]
    public string Email { get; set; } = string.Empty;
}
