using Microsoft.AspNetCore.Mvc;
using BookingWebApp.Services;
using System.Linq;
using System;
using System.Collections.Generic;
using Refit;

namespace BookingWebApp.Controllers;

public class HotelsController:Controller
{
    private readonly IApiClient _api;
    public HotelsController(IApiClient api)=>_api=api;

    public async Task<IActionResult> Index([FromQuery] string? search,int? minStars,double? maxDistance,string? sort,int page=1)
    {
        const int pageSize = 20;
        var list=await _api.Hotels(new HotelFilter(search,minStars,maxDistance,page,pageSize));

        list = sort switch
        {
            "stars_desc" => list.OrderByDescending(h => h.Stars).ToList(),
            "stars_asc" => list.OrderBy(h => h.Stars).ToList(),
            "name" => list.OrderBy(h => h.Name).ToList(),
            _ => list
        };

        ViewBag.CurrentSearch = search;
        ViewBag.CurrentMinStars = minStars;
        ViewBag.CurrentMaxDistance = maxDistance;
        ViewBag.CurrentSort = sort;
        ViewBag.Page = page;
        ViewBag.HasNext = list.Count() == pageSize;
        return View(list);
    }

    // Details: /Hotels/Details/5
    [Route("Hotels/Details/{id}")]
    public async Task<IActionResult> Details(int id,DateTime? checkIn,DateTime? checkOut)
    {
        var hotel = await _api.GetHotel(id);
        IList<RoomDto>? rooms = null;
        if(checkIn.HasValue && checkOut.HasValue){
            try{
                rooms = await _api.AvailableRooms(new AvailableFilter(id,checkIn.Value,checkOut.Value));
            }
            catch(Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest){
                // API вернул 400 — скорее всего даты невалидные.
                // Просто не показываем номера, оставив ModelState сообщение.
                ModelState.AddModelError(string.Empty, "Please select a valid date range.");
            }
        }
        var vm = new BookingWebApp.Models.HotelDetailsVm(hotel,rooms,checkIn,checkOut);
        return View(vm);
    }
} 