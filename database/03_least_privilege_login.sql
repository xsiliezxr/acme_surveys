-- Producción: login de mínimo privilegio. Sin DELETE sobre Surveys (soft delete) ni sobre Responses.
-- Reemplazar la contraseña antes de ejecutar; nunca versionarla.

USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'acme_surveys_app')
    CREATE LOGIN acme_surveys_app WITH PASSWORD = N'<<CAMBIAR-POR-SECRETO-FUERTE>>', CHECK_POLICY = ON;
GO

USE AcmeSurveys;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'acme_surveys_app')
    CREATE USER acme_surveys_app FOR LOGIN acme_surveys_app;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'acme_app_role' AND type = 'R')
    CREATE ROLE acme_app_role;
GO

GRANT SELECT                         ON dbo.FieldTypes     TO acme_app_role;
GRANT SELECT, UPDATE                 ON dbo.Users          TO acme_app_role;
GRANT SELECT, INSERT, UPDATE         ON dbo.Surveys        TO acme_app_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.SurveyFields   TO acme_app_role;
GRANT SELECT, INSERT                 ON dbo.Responses      TO acme_app_role;
GRANT SELECT, INSERT                 ON dbo.ResponseValues TO acme_app_role;
GRANT EXECUTE ON TYPE::dbo.SurveyFieldTableType   TO acme_app_role;
GRANT EXECUTE ON TYPE::dbo.ResponseValueTableType TO acme_app_role;
GO

ALTER ROLE acme_app_role ADD MEMBER acme_surveys_app;
GO
