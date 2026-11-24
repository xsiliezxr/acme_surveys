using System.Web.Mvc;

namespace AcmeSurveys.Web.Controllers
{
    public sealed class HomeController : AppControllerBase
    {
        [HttpGet, Route("")]
        public ActionResult Index() => RedirectToAction("Index", "Surveys");
    }
}
