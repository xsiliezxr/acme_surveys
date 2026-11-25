using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using AcmeSurveys.Domain.Users;
using AcmeSurveys.Infrastructure.Security;

namespace AcmeSurveys.Web
{
    public sealed class AppSettings
    {
        private const string Prefix = "Acme:";
        private const string DataProtectionKeyPrefix = Prefix + "DataProtection:Key:";
        private const string SecretsHint = " Ejecute tools\\New-DevSecrets.ps1 para generar src\\AcmeSurveys.Web\\secrets.config.";

        private static readonly Lazy<AppSettings> Instance = new Lazy<AppSettings>(Load);

        private AppSettings() { }

        public static AppSettings Current => Instance.Value;

        public string ConnectionString { get; private set; }
        public int PasswordHashIterations { get; private set; }
        public DataProtectionKeyRing DataProtectionKeyRing { get; private set; }
        public JwtOptions Jwt { get; private set; }
        public LockoutPolicy Lockout { get; private set; }
        public TimeSpan SessionIdleTimeout { get; private set; }
        public TimeSpan SessionAbsoluteTimeout { get; private set; }

        public bool SwaggerEnabled { get; private set; }

        private static AppSettings Load()
        {
            var connection = ConfigurationManager.ConnectionStrings["AcmeSurveys"];
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                throw new ConfigurationErrorsException("Falta la cadena de conexión 'AcmeSurveys' en Web.config.");

            return new AppSettings
            {
                ConnectionString = connection.ConnectionString,
                PasswordHashIterations = GetInt("PasswordHashIterations", Pbkdf2PasswordHasher.DefaultIterations),
                DataProtectionKeyRing = LoadKeyRing(),
                Jwt = new JwtOptions(
                    GetRequired("Jwt:Issuer"),
                    GetRequired("Jwt:Audience"),
                    GetBase64Key("Jwt:SigningKey", JwtOptions.MinimumKeySize),
                    TimeSpan.FromMinutes(GetInt("Jwt:LifetimeMinutes", 15))),
                Lockout = new LockoutPolicy(
                    GetInt("Lockout:MaxFailedAttempts", 5),
                    TimeSpan.FromMinutes(GetInt("Lockout:DurationMinutes", 15))),
                SessionIdleTimeout = TimeSpan.FromMinutes(GetInt("Session:IdleTimeoutMinutes", 30)),
                SessionAbsoluteTimeout = TimeSpan.FromHours(GetInt("Session:AbsoluteTimeoutHours", 8)),
                SwaggerEnabled = GetBool("Swagger:Enabled", false)
            };
        }

        private static DataProtectionKeyRing LoadKeyRing()
        {
            var keys = new Dictionary<byte, byte[]>();
            foreach (var name in ConfigurationManager.AppSettings.AllKeys.Where(k => k.StartsWith(DataProtectionKeyPrefix, StringComparison.Ordinal)))
            {
                byte id;
                if (!byte.TryParse(name.Substring(DataProtectionKeyPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out id))
                    throw new ConfigurationErrorsException("Id de clave inválido en '" + name + "'.");
                keys[id] = GetBase64Key(name.Substring(Prefix.Length), DataProtectionKeyRing.MasterKeySize);
            }

            if (keys.Count == 0)
                throw new ConfigurationErrorsException("No hay claves de cifrado configuradas (" + DataProtectionKeyPrefix + "N)." + SecretsHint);

            return new DataProtectionKeyRing((byte)GetInt("DataProtection:ActiveKeyId", keys.Keys.Max()), keys);
        }

        private static string GetRequired(string name)
        {
            var value = ConfigurationManager.AppSettings[Prefix + name];
            if (string.IsNullOrWhiteSpace(value))
                throw new ConfigurationErrorsException("Falta el valor de configuración '" + Prefix + name + "'." + SecretsHint);
            return value.Trim();
        }

        private static int GetInt(string name, int defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[Prefix + name];
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

            int value;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new ConfigurationErrorsException("'" + Prefix + name + "' debe ser un entero positivo.");
            return value;
        }

        private static bool GetBool(string name, bool defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[Prefix + name];
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

            bool value;
            if (!bool.TryParse(raw, out value))
                throw new ConfigurationErrorsException("'" + Prefix + name + "' debe ser true o false.");
            return value;
        }

        private static byte[] GetBase64Key(string name, int exactOrMinimumLength)
        {
            byte[] key;
            try
            {
                key = Convert.FromBase64String(GetRequired(name));
            }
            catch (FormatException)
            {
                throw new ConfigurationErrorsException("'" + Prefix + name + "' no es Base64 válido." + SecretsHint);
            }

            if (key.Length < exactOrMinimumLength)
                throw new ConfigurationErrorsException("'" + Prefix + name + "' debe tener al menos " + exactOrMinimumLength * 8 + " bits.");
            return key;
        }
    }
}
