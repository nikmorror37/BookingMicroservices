using CatalogService.API.Controllers;
using CatalogService.API.Domain.Models;
using CatalogService.API.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System;

namespace BookingMicro.UnitTests;

public class HotelsControllerTests
{
    private CatalogDbContext BuildContext()
    {
        var opts = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "HotelsDb_" + Guid.NewGuid())
            .Options;
        var ctx = new CatalogDbContext(opts);
        ctx.Hotels.AddRange(new List<Hotel>
        {
            new Hotel { Id = 1, Name = "Hilton",  City = "Warsaw", Country="Poland", Address="Street 1", Stars=4, DistanceFromCenter=1.0 },
            new Hotel { Id = 2, Name = "Ibis",    City = "Warsaw", Country="Poland", Address="Street 2", Stars=3, DistanceFromCenter=2.0 },
            new Hotel { Id = 3, Name = "Crowne Plaza",  City = "Berlin", Country="Germany", Address="Street 3", Stars=5, DistanceFromCenter=0.5 }
        });
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task Get_without_filters_returns_all()
    {
        var ctx = BuildContext();
        var ctrl = new HotelsController(ctx);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };
        var result = await ctrl.Get();
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var list = ok!.Value as IEnumerable<Hotel>;
        list!.Should().HaveCount(3);
    }

    [Fact]
    public async Task Get_with_minStars_filters()
    {
        var ctx = BuildContext();
        var ctrl = new HotelsController(ctx);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };
        var result = await ctrl.Get(minStars:4);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IEnumerable<Hotel>;
        list!.Should().OnlyContain(h => h.Stars >=4);
    }

    [Fact]
    public async Task Get_with_search_filters_by_city_or_name()
    {
        var ctx = BuildContext();
        var ctrl = new HotelsController(ctx);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };
        var result = await ctrl.Get(search:"Warsaw");
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IEnumerable<Hotel>;
        list!.Should().OnlyContain(h => h.City.Contains("Warsaw") || h.Name.Contains("Warsaw"));
    }
} 