﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ProjektMVC.Models;
using Microsoft.AspNetCore.Authorization;
using ProjektAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace ProjektMVC.Controllers
{
    public class LogowanieController : Controller
    {

        private List<UzytkownikModel> _uzytkownicy = null;
        private readonly HttpClient client;
        private readonly string UzytkownikPath;
        private readonly IConfiguration _configuration;

        public LogowanieController(IConfiguration configuration)
        {
            _configuration = configuration;
            UzytkownikPath = _configuration["ProjektAPIConfig:Url4"];
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("ApiKey", _configuration["ProjektAPIConfig:ApiKey"]);
        }

        private async Task<ActionResult> Uzytkownicy()
        {
            HttpResponseMessage response = await client.GetAsync(UzytkownikPath);
            if(response.IsSuccessStatusCode)
            {
                _uzytkownicy = await response.Content.ReadAsAsync<List<UzytkownikModel>>();
            }
            return View(_uzytkownicy);
        }

        [Authorize]
        public IActionResult Informacje()
        {
            return View();
        }

        
        public IActionResult Login(string url = "/")
        {
            if (User.Identity.IsAuthenticated)
                HttpContext.Response.Redirect("/");
            LoginModel model = new()
            {
                URL = url
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var uzytkownik = _uzytkownicy.Where(x => x.Login == model.Login && x.Haslo == model.Haslo).FirstOrDefault();
                if (uzytkownik == null)
                {
                    ViewBag.Message = "Podane dane nie są prawidłowe.";
                    return View(model);
                }
                else
                {
                    var claims = new List<Claim>()
                    {
                        new Claim(ClaimTypes.NameIdentifier, uzytkownik.Id.ToString()),
                        new Claim(ClaimTypes.Name, uzytkownik.Login),
                        new Claim(ClaimTypes.Role, uzytkownik.RodzajUzytkownika.ToString())

                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal, new AuthenticationProperties() { IsPersistent = model.PamietajMnie });

                    return LocalRedirect(model.URL);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/");
        }

    }
}
