using Microsoft.AspNetCore.Mvc;
using BookingWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace BookingWebApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Hotels")]
public class AdminHotelsController : Controller
{
    private readonly IApiClient _api;
    public AdminHotelsController(IApiClient api) { _api = api; }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var hotel = await _api.GetHotel(id);
        var vm = new HotelEditVm
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            Stars = hotel.Stars,
            DistanceFromCenter = hotel.DistanceFromCenter,
            Description = hotel.Description,
            ImageUrl = hotel.ImageUrl
        };
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    public async Task<IActionResult> Edit(int id, HotelEditVm vm, IFormFile? image)
    {
        if (!ModelState.IsValid)
            return View(vm);

        if (image != null && image.Length > 0)
        {
            var res = await _api.UploadImage(image, id);
            vm.ImageUrl = res.ImageUrl;
        }

        var req = new HotelUpdateRequest(vm.Id, vm.Name, vm.Address, vm.City, vm.Country, vm.Stars, vm.DistanceFromCenter, vm.ImageUrl, vm.Description);
        await _api.UpdateHotel(id, req);
        return RedirectToAction("Details", "Hotels", new { id });
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var hotels = await _api.Hotels(new HotelFilter(null, null, null));
        return View("Index", hotels);
    }

    [HttpGet("Create")]
    public IActionResult Create() => View("Create", new HotelEditVm());

    [HttpPost("Create")]
    public async Task<IActionResult> Create(HotelEditVm vm, IFormFile? image)
    {
        if (!ModelState.IsValid) return View("Create", vm);

        // create hotel without ImageUrl
        var created = await _api.CreateHotel(
            new HotelUpdateRequest(0, vm.Name, vm.Address, vm.City, vm.Country,
                               vm.Stars, vm.DistanceFromCenter, null, vm.Description));

        if (image != null && image.Length > 0)
        {
            var res = await _api.UploadImage(image, created.Id);
            await _api.UpdateHotel(created.Id,
            new HotelUpdateRequest(created.Id, vm.Name, vm.Address, vm.City, vm.Country,
                                   vm.Stars, vm.DistanceFromCenter, res.ImageUrl, vm.Description));
        }
        return RedirectToAction("Index");
    }

    [HttpPost("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _api.DeleteHotel(id);
        return RedirectToAction("Index");
    }
    
    // method for loading additional images via AJAX
    [HttpPost("{id}/UploadImages")]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile> files)
    {
        // check that the files have been transferred
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded");

        try
        {
            // call API to upload images to the hotel folder
            var result = await _api.UploadAdditionalImages(id, files);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

}

public class HotelEditVm
{
    public int Id { get; set; }
    public string Name {get;set;} = null!;
    public string Address {get;set;} = null!;
    public string City {get;set;} = null!;
    public string Country {get;set;} = null!;
    public int Stars {get;set;}
    public double DistanceFromCenter {get;set;}
    public string? ImageUrl {get;set;}
    public string? Description {get;set;}
} 