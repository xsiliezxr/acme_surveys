using System.Web.Mvc;
using AcmeSurveys.Web.Security;

namespace AcmeSurveys.Web.Controllers
{
    public abstract class AppControllerBase : Controller
    {
        public const string SuccessMessageKey = "SuccessMessage";

        protected int CurrentUserId => User.GetUserId();

        protected string ClientIp => Request.UserHostAddress;

        protected ActionResult NotFoundView() => StatusView(404, "NotFound");

        protected ActionResult StatusView(int statusCode, string viewName)
        {
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;
            return View(viewName);
        }

        protected void FlashSuccess(string message) => TempData[SuccessMessageKey] = message;
    }
}
