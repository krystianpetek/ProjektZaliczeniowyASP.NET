﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjektAPI.Models;
using ProjektMVC.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjektMVC.Controllers
{
    public class LogowanieController : Controller
    {
        private List<UzytkownikModel> _uzytkownicy = null;
        private readonly HttpClient client;
        private readonly string KlientPath;
        private readonly string UzytkownikPath;
        private readonly IConfiguration _configuration;

        public LogowanieController(IConfiguration configuration)
        {
            _configuration = configuration;
            KlientPath = _configuration["ProjektAPIConfig:Klient"];
            UzytkownikPath = _configuration["ProjektAPIConfig:Login"];
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("ApiKey", _configuration["ProjektAPIConfig:ApiKey"]);
        }

        [Authorize]
        public async Task<ActionResult> Informacje()
        {
            List<KlientModel> listaKlientow = default;
            KlientModel klient = default;
            HttpResponseMessage response = await client.GetAsync(KlientPath);
            if (response.IsSuccessStatusCode)
            {
                listaKlientow = await response.Content.ReadAsAsync<List<KlientModel>>();
                klient = listaKlientow.FirstOrDefault(q => q.Uzytkownik.Login == User.Identity.Name);
            }

            return View(klient);
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
                HttpResponseMessage response = await client.GetAsync(UzytkownikPath);
                if (response.IsSuccessStatusCode)
                {
                    _uzytkownicy = await response.Content.ReadAsAsync<List<UzytkownikModel>>();
                }

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

                    return LocalRedirect("/");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/");
        }

        [HttpGet]
        public IActionResult Rejestracja()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejestracja([Bind] KlientModel model)
        {
            model.Uzytkownik.RodzajUzytkownika = Rola.Klient;
            if (ModelState.IsValid)
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(KlientPath, model);
                response.EnsureSuccessStatusCode();
                ViewBag.Register = $"{model.Imie}, rejestracja udana! Możesz się zalogować.";
                return RedirectToAction("Login", model);
            }
            ViewBag.Register = "Błąd, spróbuj ponownie";
            return BadRequest();
        }
    }
}