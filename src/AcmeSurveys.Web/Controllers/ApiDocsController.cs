using System.Web.Mvc;

namespace AcmeSurveys.Web.Controllers
{
    [AllowAnonymous]
    public sealed class ApiDocsController : AppControllerBase
    {
        private readonly AppSettings _settings;

        public ApiDocsController(AppSettings settings)
        {
            _settings = settings;
        }

        [HttpGet, Route(SwaggerConfig.UiPath)]
        public ActionResult Index() => _settings.SwaggerEnabled ? View() : NotFoundView();
    }
}
