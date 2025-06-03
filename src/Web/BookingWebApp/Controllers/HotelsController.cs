using Microsoft.AspNetCore.Mvc;
using BookingWebApp.Services;
using System.Linq;
using System;
using System.Collections.Generic;
using Refit;

namespace BookingWebApp.Controllers;

public class HotelsController : Controller
{
    private readonly IApiClient _api;
    public HotelsController(IApiClient api) => _api = api;

    public async Task<IActionResult> Index([FromQuery] string? search, int? minStars, double? maxDistance, string? sort, int page = 1)
    {
        try
        {
            const int pageSize = 4;
            var list = await _api.Hotels(new HotelFilter(search, minStars, maxDistance, page, pageSize, sort));

            if (page > 1 && list.Count == 0)
            return RedirectToAction(nameof(Index),
                new { search, minStars, maxDistance, sort, page = page - 1 });

            var nextPage = await _api.Hotels(new HotelFilter(search, minStars, maxDistance, page + 1, 1, sort));
            bool hasNext = nextPage.Any();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentMinStars = minStars;
            ViewBag.CurrentMaxDistance = maxDistance;
            ViewBag.CurrentSort = sort;
            ViewBag.Page = page;
            ViewBag.HasNext = hasNext;
            return View(list);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Unable to connect to the server. Please try again later.";
            return View(new List<HotelDto>());
        }
        catch (TaskCanceledException)
        {
            TempData["Error"] = "The request timed out. Please try again.";
            return View(new List<HotelDto>());
        }
        catch (Exception)
        {
            TempData["Error"] = "An unexpected error occurred. Please try again.";
            return View(new List<HotelDto>());
        }
    }

    // Details: /Hotels/Details/{id}
    [Route("Hotels/Details/{id}")]
    public async Task<IActionResult> Details(int id, DateTime? checkIn, DateTime? checkOut)
    {
        var hotel = await _api.GetHotel(id);
        IList<RoomDto>? rooms = null;
        if (checkIn.HasValue && checkOut.HasValue)
        {
            if (HttpContext.Session.GetString("jwt") is null)
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                rooms = await _api.AvailableRooms(new AvailableFilter(id, checkIn.Value, checkOut.Value));
            }
            catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                // The API returned 400, which are probably non-valid dates.
                // just don’t show the rooms, leaving ModelState message.
                ModelState.AddModelError(string.Empty, "Please select a valid date range.");
            }
        }
        // get all images of the hotel
        var additionalImages = await _api.GetHotelImagesAsync(id);

        var vm = new BookingWebApp.Models.HotelDetailsVm(hotel, rooms, checkIn, checkOut)
        {
            AdditionalImages = additionalImages
        };
        return View(vm);
    }
    
    // retrieve hotel images via AJAX
    // used in admin dashboard to display existing images
    [HttpGet("GetImages/{id}")]
    public async Task<IActionResult> GetImages(int id)
    {
        // call the API to get a list of hotel images
        var images = await _api.GetHotelImagesAsync(id);
        // return the list as JSON for JS
        return Json(images);
    }
} 