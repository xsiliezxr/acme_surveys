using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;

namespace AcmeSurveys.Web.Common
{
    // TempData en cookie cifrada con MachineKey: la app no usa Session State.
    public sealed class CookieTempDataProvider : ITempDataProvider
    {
        public const string CookieName = "__Host-AcmeTempData";
        private const string ProtectionPurpose = "AcmeSurveys.TempData.v1";
        private const int MaxCookieValueLength = 3800;

        public IDictionary<string, object> LoadTempData(ControllerContext controllerContext)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var cookie = controllerContext.HttpContext.Request.Cookies[CookieName];
            if (string.IsNullOrEmpty(cookie?.Value))
            {
                return values;
            }

            try
            {
                var protectedBytes = HttpServerUtility.UrlTokenDecode(cookie.Value);
                if (protectedBytes == null)
                {
                    return values;
                }

                var json = Encoding.UTF8.GetString(MachineKey.Unprotect(protectedBytes, ProtectionPurpose));
                var stored = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (stored != null)
                {
                    foreach (var pair in stored)
                    {
                        values[pair.Key] = pair.Value;
                    }
                }
            }
            catch (Exception ex) when (ex is CryptographicException || ex is JsonException || ex is FormatException)
            {
            }

            return values;
        }

        public void SaveTempData(ControllerContext controllerContext, IDictionary<string, object> values)
        {
            var httpContext = controllerContext.HttpContext;

            if (values == null || values.Count == 0)
            {
                if (httpContext.Request.Cookies[CookieName] != null)
                {
                    WriteCookie(httpContext, string.Empty, DateTime.UtcNow.AddYears(-1));
                }
                return;
            }

            var serializable = new Dictionary<string, string>(values.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values)
            {
                if (pair.Value != null && !(pair.Value is string))
                {
                    throw new InvalidOperationException("TempData solo admite cadenas (clave '" + pair.Key + "').");
                }
                serializable[pair.Key] = (string)pair.Value;
            }

            var plaintext = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serializable));
            var cookieValue = HttpServerUtility.UrlTokenEncode(MachineKey.Protect(plaintext, ProtectionPurpose));
            if (cookieValue.Length > MaxCookieValueLength)
            {
                throw new InvalidOperationException("El contenido de TempData excede el tamaño máximo de una cookie.");
            }

            WriteCookie(httpContext, cookieValue, expiresUtc: null);
        }

        private static void WriteCookie(HttpContextBase httpContext, string value, DateTime? expiresUtc)
        {
            var cookie = new HttpCookie(CookieName, value)
            {
                HttpOnly = true,
                Secure = true,
                Path = "/",
                SameSite = SameSiteMode.Lax
            };
            if (expiresUtc.HasValue)
            {
                cookie.Expires = expiresUtc.Value;
            }
            httpContext.Response.Cookies.Set(cookie);
        }
    }
}
