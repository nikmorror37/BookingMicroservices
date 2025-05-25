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
    public async Task<IActionResult> Index(int page=1)
    {
        const int pageSize = 10;
        try
        {
            var list = await _api.MyBookings(page,pageSize);
            ViewBag.Page = page;
            ViewBag.HasNext = list.Count==pageSize;
            return View(list);
        }
        catch(ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login","Account");
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
        }
        catch(ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login","Account");
        }
        return RedirectToAction("Confirmed", new { id });
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
        // Оплачивать можно, только если бронь ещё ожидает оплаты
        if(booking.Status!= (int)BookingStatus.Pending && booking.Status != (int)BookingStatus.RefundError)
            return RedirectToAction("Details", new { id });

        var nights = Math.Max(1, (booking.CheckOut - booking.CheckIn).Days);
        // Цена теперь уже есть в booking.Room.Price (Room объект приходит с backend)
        var amount = booking.Room?.Price * nights ?? 0;

        var vm = new PaymentVm(booking, amount);
        return View("Payment", vm);
    }
} 