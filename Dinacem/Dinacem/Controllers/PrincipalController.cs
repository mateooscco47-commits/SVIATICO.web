using Microsoft.AspNetCore.Mvc;

namespace Dinacem.Controllers
{
    public class PrincipalController : Controller
    {
        // Panel principal del administrador
        public IActionResult Index()
        {
            return View();
        }
    }
}