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

namespace ProjektMVC.Controllers
{
    public class LogowanieController : Controller
    {
        private APIDatabaseContext _context;
        private readonly List<UzytkownikModel> _uzytkownicy;
        public LogowanieController(APIDatabaseContext context)
        {
            _context = context;
            _uzytkownicy = _context.Login.ToList();

            if (!_context.Klienci.Any())
            {
                _context.Klienci.AddRange(
                    new KlientModel()
                    {
                        Imie = "Krystian",
                        Nazwisko = "Petek",
                        DataUrodzenia = new System.DateTime(1998, 10, 06),
                        Miasto = "Koziniec",
                        Ulica = "2",
                        NumerTelefonu = "884284782",
                        KodPocztowy = "34-106",
                        Email = "krystianpetek2@gmail.com",
                        Uzytkownik = new UzytkownikModel()
                        {
                            Login = "krystianpetek",
                            Haslo = "qwerty123",
                            RodzajUzytkownika = Rola.Admin
                        }
                    },
                new KlientModel()
                {
                    Imie = "Gabriel",
                    Nazwisko = "Warchał",
                    DataUrodzenia = new System.DateTime(1993, 03, 20),
                    Miasto = "Świnna Poręba",
                    Ulica = "158",
                    NumerTelefonu = "889410340",
                    KodPocztowy = "34-106",
                    Email = "mr.warchal@gmail.com",
                    Uzytkownik = new UzytkownikModel()
                    {
                        Login = "gabrys.158",
                        Haslo = "123qweasdzxc",
                        RodzajUzytkownika = Rola.Klient
                    }
                });
            }
                if (!_context.SaleKinowe.Any())
                {
                    _context.SaleKinowe.AddRange(
                        new SalaModel()
                        {
                            NazwaSali = "Sala 1",
                            IloscMiejsc = 10,
                            IloscRzedow = 10

                        },
                        new SalaModel()
                        {
                            NazwaSali = "Sala 2",
                            IloscMiejsc = 12,
                            IloscRzedow = 8
                        });
                }
            
            if (!_context.Filmy.Any())
                {
                    _context.Filmy.AddRange(
                        new FilmModel()
                        {
                            Nazwa = "Skazani na Shawshank",
                            Opis = "Adaptacja opowiadania Stephena Kinga. Niesłusznie skazany na dożywocie bankier, stara się przetrwać w brutalnym, więziennym świecie.",
                            Gatunek = "Dramat",
                            CzasTrwania = "2 godz. 22 min.",
                            OgraniczeniaWiek = Wiek.Od16lat,
                            Cena = 35
                        },
                        new FilmModel()
                        {
                            Nazwa = "Nietykalni",
                            Opis = "Sparaliżowany milioner zatrudnia do opieki młodego chłopaka z przedmieścia, który właśnie wyszedł z więzienia.",
                            Gatunek = "Biograficzny, Dramat, Komedia",
                            CzasTrwania = "1 godz. 52 min.",
                            OgraniczeniaWiek = Wiek.Od12lat,
                            Cena = 40
                        });
                }
                _context.SaveChanges();
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
                    ViewBag.Message = "Provided crediential is not valid.";
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
