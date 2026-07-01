using Microsoft.AspNetCore.Mvc;

namespace Dinacem.Controllers
{
    public class EmpleadoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}