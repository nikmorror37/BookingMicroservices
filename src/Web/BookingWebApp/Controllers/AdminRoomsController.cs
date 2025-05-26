using Microsoft.AspNetCore.Mvc;
using BookingWebApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace BookingWebApp.Controllers;

[Authorize(Roles="Admin")]
[Route("Admin/Rooms")]
public class AdminRoomsController:Controller
{
    private readonly IApiClient _api;
    public AdminRoomsController(IApiClient api){_api=api;}

    [HttpGet("")]
    public async Task<IActionResult> Index(int? hotelId)
    {
        var hotels = await _api.Hotels(new HotelFilter(null,null,null));
        ViewBag.Hotels = hotels;
        var rooms = await _api.Rooms(new RoomFilter(hotelId,null,null,null));
        return View("Index", rooms);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Hotels = await _api.Hotels(new HotelFilter(null,null,null));
        ViewBag.IsCreate = true;
        return View("Create", new RoomEditVm());
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(RoomEditVm vm, IFormFile? image)
    {
        if(!ModelState.IsValid){ViewBag.Hotels = await _api.Hotels(new HotelFilter(null,null,null));return View("Create",vm);}
        if (image != null && image.Length > 0)
        {
            var res = await _api.UploadImage(image);
            vm.RoomImageUrl = res.ImageUrl;
            //vm.RoomImageUrl = res.ImageUrl.StartsWith("/")?"http://localhost:8080"+res.ImageUrl:res.ImageUrl;
        }
        await _api.CreateRoom(ToReq(vm));
        return RedirectToAction("Index",new{hotelId=vm.HotelId});
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _api.GetRoom(id);
        var vm = new RoomEditVm{Id = room.Id,HotelId=room.HotelId,Number=room.Number,Type=room.Type,Price=room.Price,Description=room.Description, IsAvailable=room.IsAvailable,RoomImageUrl=room.RoomImageUrl,Capacity=room.Capacity,NumberOfBeds=room.NumberOfBeds};
        ViewBag.Hotels = await _api.Hotels(new HotelFilter(null,null,null));
        ViewBag.IsCreate=false;
        return View("Edit",vm);
    }

    [HttpPost("Edit/{id}")]
    public async Task<IActionResult> Edit(int id,RoomEditVm vm,IFormFile? image)
    {
        if(!ModelState.IsValid){ViewBag.Hotels = await _api.Hotels(new HotelFilter(null,null,null));return View("Edit",vm);}
        if (image != null && image.Length > 0)
        {
            var res = await _api.UploadImage(image);
            vm.RoomImageUrl = res.ImageUrl;
            //vm.RoomImageUrl= res.ImageUrl.StartsWith("/")?"http://localhost:8080"+res.ImageUrl:res.ImageUrl;
        }
        await _api.UpdateRoom(id,ToReq(vm));
        return RedirectToAction("Index",new{hotelId=vm.HotelId});
    }

    [HttpPost("Delete/{id}")]
    public async Task<IActionResult> Delete(int id,int hotelId)
    {
        await _api.DeleteRoom(id);
        return RedirectToAction("Index",new{hotelId});
    }

    private static RoomUpdateRequest ToReq(RoomEditVm v)=> new(v.Id,v.HotelId,v.Number,v.Type,v.Price,v.Description,v.Capacity,v.NumberOfBeds,v.IsAvailable,v.RoomImageUrl);
}

public class RoomEditVm{
    public int Id{get;set;}
    public int HotelId{get;set;}
    public string Number{get;set;}=null!;
    public RoomType Type{get;set;}
    public decimal Price{get;set;}
    public string? Description{get;set;}
    public int Capacity{get;set;}
    public int NumberOfBeds{get;set;}
    public bool IsAvailable{get;set;}=true;
    public string? RoomImageUrl{get;set;}
} 