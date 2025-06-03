using System;
using System.Collections.Generic;
using BookingWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Refit;
using BookingWebApp.Models;
using System.Linq;

namespace BookingWebApp.Controllers;

public class BookingsController : Controller
{
    private readonly IApiClient _api;
    private readonly IHttpContextAccessor _ctx;
    public BookingsController(IApiClient api,IHttpContextAccessor ctx){_api=api;_ctx=ctx;}

    // GET /Bookings/Search
    [HttpGet]
    public async Task<IActionResult> Search(int? hotelId,DateTime? checkIn,DateTime? checkOut)
    {
        var hotels = await _api.Hotels(new HotelFilter(null,null,null));
        IList<RoomDto>? rooms = null;
        if(hotelId.HasValue && checkIn.HasValue && checkOut.HasValue)
        {
            try
            {
                rooms = await _api.AvailableRooms(new AvailableFilter(hotelId.Value,checkIn.Value,checkOut.Value));
            }
            catch(ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login","Account");
            }
            ViewBag.SelectedHotelId = hotelId.Value;
            ViewBag.CheckIn = checkIn.Value.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = checkOut.Value.ToString("yyyy-MM-dd");
        }
        ViewBag.Hotels = hotels;
        return View(rooms??new List<RoomDto>());
    }

    // POST /Bookings/Book
    [HttpPost]
    public async Task<IActionResult> Book(int hotelId,int roomId,DateTime checkIn,DateTime checkOut)
    {
        BookingDto booking;
        try
        {
            booking = await _api.CreateBooking(new NewBookingDto(hotelId,roomId,checkIn,checkOut));
        }
        catch(ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login","Account");
        }
        return RedirectToAction("Summary", new{ id = booking.Id });
    }

    // GET /Bookings
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 9;
        const int fetchSize = pageSize + 1;
        try
        {
            var items = await _api.MyBookings(page, fetchSize);

            if (page > 1 && !items.Any())
                return RedirectToAction(nameof(Index), new { page = page - 1 });

            bool hasNext = items.Count > pageSize;

            var list = items.Take(pageSize).ToList();
            
            ViewBag.Page = page;
            ViewBag.HasNext = hasNext;
            return View(list);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (Exception) 
        {
            TempData["Error"] = "An unexpected error occurred while fetching your bookings. Please try again.";
            return View(new List<BookingDto>()); 
        }
    }

    // GET /Bookings/Details/{id}
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var dto = await _api.GetBookingById(id);
            return View(dto);
        }
        catch(ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login","Account");
        }
    }

    // GET /Bookings/Summary/{id}
    [HttpGet]
    public async Task<IActionResult> Summary(int id)
    {
        var dto = await _api.GetBookingById(id);
        return View(dto);
    }

    // POST /Bookings/Confirm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirm(int id,string firstName,string lastName,string email)
    {
        // Guest data could be forwarded to API later; for demo we just keep in TempData
        TempData["Toast"] = "Guest details saved. Please complete payment.";
        return RedirectToAction("Checkout", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int id)
    {
        try
        {
            await _api.PayBooking(id);
            return RedirectToAction("Confirmed", new { id });
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            TempData["Error"] = "Payment processing failed. Please check your payment details and try again.";
            return RedirectToAction("Checkout", new { id });
        }
    }

    // GET /Bookings/Confirmed/{id}
    [HttpGet]
    public async Task<IActionResult> Confirmed(int id)
    {
        var dto = await _api.GetBookingById(id);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await _api.CancelBooking(id);
        }
        catch(ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login","Account");
        }
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Checkout(int id)
    {
        var booking = await _api.GetBookingById(id);
        // can only pay if the booking is still pending
        if(booking.Status!= (int)BookingStatus.Pending && booking.Status != (int)BookingStatus.RefundError)
            return RedirectToAction("Details", new { id });

        var nights = Math.Max(1, (booking.CheckOut - booking.CheckIn).Days);
        // price is now in booking.Room.Price (Room object comes with backend)
        var amount = booking.Room?.Price * nights ?? 0;

        var vm = new PaymentVm(booking, amount);
        return View("Payment", vm);
    }
} 