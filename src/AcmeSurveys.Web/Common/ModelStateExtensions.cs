using System.Web.Mvc;
using AcmeSurveys.Domain.Common;

namespace AcmeSurveys.Web.Common
{
    public static class ModelStateExtensions
    {
        public static void AddResultErrors(this ModelStateDictionary modelState, Result result, string prefix = null)
        {
            foreach (var error in result.Errors)
            {
                var key = string.IsNullOrEmpty(error.Target)
                    ? string.Empty
                    : string.IsNullOrEmpty(prefix) ? error.Target : prefix + error.Target;
                modelState.AddModelError(key, error.Message);
            }
        }
    }
}
