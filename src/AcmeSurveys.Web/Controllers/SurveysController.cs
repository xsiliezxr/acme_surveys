using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using AcmeSurveys.Application.Results;
using AcmeSurveys.Application.Surveys;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Web.Common;
using AcmeSurveys.Web.Models;

namespace AcmeSurveys.Web.Controllers
{
    [RoutePrefix("surveys")]
    public sealed class SurveysController : AppControllerBase
    {
        private const string FormView = "Form";

        private readonly SurveyManagementService _surveys;
        private readonly SurveyResultsService _results;

        public SurveysController(SurveyManagementService surveys, SurveyResultsService results)
        {
            _surveys = surveys;
            _results = results;
        }

        [HttpGet, Route("")]
        public async Task<ActionResult> Index()
        {
            return View(await _surveys.ListAsync(CurrentUserId));
        }

        [HttpGet, Route("new")]
        public ActionResult Create()
        {
            return View(FormView, SurveyFormViewModel.ForCreate());
        }

        [HttpPost, Route("new")]
        public async Task<ActionResult> Create(SurveyFormViewModel model)
        {
            model.ExternalId = null;
            var result = await _surveys.CreateAsync(CurrentUserId, model.ToCommand());
            if (result.IsFailure)
            {
                ModelState.AddResultErrors(result);
                return View(FormView, model);
            }

            FlashSuccess("Encuesta creada. Ya puede compartir su link.");
            return RedirectToAction("Index");
        }

        [HttpGet, Route("{id:guid}/edit")]
        public async Task<ActionResult> Edit(Guid id)
        {
            var result = await _surveys.GetAsync(CurrentUserId, id);
            if (result.IsFailure) return NotFoundView();
            return View(FormView, SurveyFormViewModel.FromDetails(result.Value));
        }

        [HttpPost, Route("{id:guid}/edit")]
        public async Task<ActionResult> Edit(Guid id, SurveyFormViewModel model)
        {
            model.ExternalId = id;
            var result = await _surveys.UpdateAsync(CurrentUserId, id, model.ToCommand());
            if (result.HasError(ErrorCodes.NotFound)) return NotFoundView();
            if (result.IsFailure)
            {
                ModelState.AddResultErrors(result);
                return View(FormView, model);
            }

            FlashSuccess("Cambios guardados.");
            return RedirectToAction("Index");
        }

        [HttpGet, Route("{id:guid}/delete")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _surveys.GetAsync(CurrentUserId, id);
            if (result.IsFailure) return NotFoundView();
            return View(result.Value);
        }

        [HttpPost, Route("{id:guid}/delete")]
        [ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _surveys.DeleteAsync(CurrentUserId, id);
            if (result.IsFailure) return NotFoundView();

            FlashSuccess("Encuesta eliminada. Su link público dejó de funcionar.");
            return RedirectToAction("Index");
        }

        [HttpPost, Route("{id:guid}/rotate-link")]
        public async Task<ActionResult> RotateLink(Guid id)
        {
            var result = await _surveys.RotatePublicLinkAsync(CurrentUserId, id);
            if (result.HasError(ErrorCodes.NotFound)) return NotFoundView();

            if (result.IsSuccess) FlashSuccess("Se generó un link nuevo. El anterior ya no funciona.");
            return RedirectToAction("Index");
        }

        [HttpGet, Route("{id:guid}/results")]
        public async Task<ActionResult> Results(Guid id, int page = 1)
        {
            var result = await _results.GetPageAsync(CurrentUserId, id, page, SurveyResultsService.DefaultPageSize);
            if (result.IsFailure) return NotFoundView();
            return View(result.Value);
        }
    }
}
