using System.Web.Mvc;

namespace AcmeSurveys.Web.Controllers
{
    public sealed class ErrorsController : AppControllerBase
    {
        [AllowAnonymous]
        public ActionResult NotFound() => NotFoundView();
    }
}
