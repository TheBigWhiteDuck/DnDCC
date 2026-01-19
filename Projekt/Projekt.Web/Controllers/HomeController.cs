using System.Diagnostics;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Projekt.ViewModel.VM;

namespace Projekt.Web.Controllers
{
    /// <summary>
    /// Kontroler strony głównej i stron statycznych (Privacy, Contact, Faq, Dice, Premium).
    /// </summary>
    public class HomeController : BaseController
    {
        /// <summary>
        /// Tworzy instancję HomeController i przekazuje zależności do BaseController.
        /// </summary>
        public HomeController(ILogger logger, IMapper mapper, IStringLocalizer localizer)
            : base(logger, mapper, localizer) { }

        /// <summary>
        /// Wyświetla widok główny.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok strony domowej.
        /// </summary>
        public IActionResult HomePage()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok polityki prywatności.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok strony kontaktowej.
        /// </summary>
        public IActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok FAQ.
        /// </summary>
        public IActionResult Faq()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok rzutu kostką (dice).
        /// </summary>
        public IActionResult Dice()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok strony premium.
        /// </summary>
        public IActionResult Premium()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla widok błędu wraz z RequestId.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }
    }
}
