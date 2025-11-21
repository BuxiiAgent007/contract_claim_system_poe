using Microsoft.AspNetCore.Mvc;

namespace contract_claim_system.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
