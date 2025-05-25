using Microsoft.AspNetCore.Mvc;
using BookingWebApp.Services;
using Microsoft.AspNetCore.Http;
using Refit;
using System.Net;

namespace BookingWebApp.Controllers;

public class AccountController : Controller
{
    private readonly IApiClient _api;
    private readonly IHttpContextAccessor _ctx;
    public AccountController(IApiClient api,IHttpContextAccessor ctx){_api=api;_ctx=ctx;}

    [HttpGet]
    public IActionResult Login()=>View();

    [HttpPost]
    public async Task<IActionResult> Login(string email,string password)
    {
        try{
            var res=await _api.Login(new LoginRequest(email,password));
            _ctx.HttpContext!.Session.SetString("jwt",res.Token);
            return RedirectToAction("Index","Hotels");
        }
        catch(ApiException ex) when (ex.StatusCode==HttpStatusCode.BadRequest || ex.StatusCode==HttpStatusCode.Unauthorized)
        {
            ModelState.AddModelError(string.Empty,"Invalid email or password.");
            ViewBag.Email=email;
            return View();
        }
        catch(HttpRequestException){
            ModelState.AddModelError(string.Empty,"Invalid email or password.");
            ViewBag.Email=email;
            return View();
        }
    }

    [HttpGet]
    public IActionResult Register()=>View();

    [HttpPost]
    public async Task<IActionResult> Register(string email,string password,string firstName,string lastName)
    {
        try
        {
            await _api.Register(new RegisterRequest(email,password,firstName,lastName));
            return RedirectToAction("Login");
        }
        catch(ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(string.Empty, ex.Content);
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var me = await _api.Me();
            return View(me);
        }
        catch(ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _ctx.HttpContext!.Session.Remove("jwt");
            return RedirectToAction("Login");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        try
        {
            var me = await _api.Me();
            var vm = new BookingWebApp.Models.EditProfileVm
            {
                FirstName  = me.FirstName,
                LastName   = me.LastName,
                Address    = me.Address,
                City       = me.City,
                State      = me.State,
                PostalCode = me.PostalCode,
                Country    = me.Country,
                DateOfBirth = me.DateOfBirth
            };
            return View(vm);
        }
        catch(ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _ctx.HttpContext!.Session.Remove("jwt");
            return RedirectToAction("Login");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BookingWebApp.Models.EditProfileVm vm)
    {
        if(!ModelState.IsValid)
            return View(vm);

        try
        {
            await _api.EditProfile(new UpdateProfileRequest(vm.FirstName,vm.LastName,vm.Address,vm.City,vm.State,vm.PostalCode,vm.Country,vm.DateOfBirth));
            TempData["ProfileUpdated"] = true;
            return RedirectToAction("Profile");
        }
        catch(ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _ctx.HttpContext!.Session.Remove("jwt");
            return RedirectToAction("Login");
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _ctx.HttpContext!.Session.Remove("jwt");
        return RedirectToAction("Login");
    }
} 