using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using AcmeSurveys.Application.Responses;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Web.Common;
using AcmeSurveys.Web.Models;

namespace AcmeSurveys.Web.Controllers
{
    [AllowAnonymous]
    [RoutePrefix(PublicLinks.RoutePrefix)]
    public sealed class PublicSurveysController : AppControllerBase
    {
        private readonly PublicSurveyService _publicSurveys;

        public PublicSurveysController(PublicSurveyService publicSurveys)
        {
            _publicSurveys = publicSurveys;
        }

        [HttpGet, Route("{token:guid}")]
        public async Task<ActionResult> Fill(Guid token)
        {
            var form = await _publicSurveys.GetFormAsync(token);
            if (form.IsFailure) return UnavailableView();
            return View(new FillSurveyViewModel(form.Value));
        }

        [HttpPost, Route("{token:guid}")]
        [ActionName("Fill")]
        public async Task<ActionResult> Submit(Guid token)
        {
            var values = ReadAnswers();
            var result = await _publicSurveys.SubmitAsync(token, values);
            if (result.HasError(ErrorCodes.NotFound)) return UnavailableView();

            if (result.IsFailure)
            {
                ModelState.AddResultErrors(result, FillSurveyViewModel.InputPrefix);
                var form = await _publicSurveys.GetFormAsync(token);
                if (form.IsFailure) return UnavailableView();
                return View(new FillSurveyViewModel(form.Value, values));
            }

            return RedirectToAction("Thanks");
        }

        [HttpGet, Route("thanks")]
        public ActionResult Thanks()
        {
            return View();
        }

        private IReadOnlyDictionary<string, string> ReadAnswers()
        {
            var form = Request.Unvalidated.Form;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in form.AllKeys)
            {
                if (key != null && key.StartsWith(FillSurveyViewModel.InputPrefix, StringComparison.Ordinal))
                    values[key.Substring(FillSurveyViewModel.InputPrefix.Length)] = form[key];
            }
            return values;
        }

        private ActionResult UnavailableView() => StatusView(404, "Unavailable");
    }
}
