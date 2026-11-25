-- Datos de demostración. Hashes PBKDF2-HMAC-SHA256 (600 000 iteraciones); credenciales en el README.

USE AcmeSurveys;
GO
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
    INSERT INTO dbo.Users (ExternalId, Username, PasswordHash, PasswordSalt, PasswordIterations)
    VALUES (NEWID(), N'admin',
            0xCC9572694F6D1E317C573B1D4F42841098FC9CAC9399CBDBAFC1332CF84FA5A2,
            0x170376C05F923D36DCB4C71E59FBFC80,
            600000);

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'analista')
    INSERT INTO dbo.Users (ExternalId, Username, PasswordHash, PasswordSalt, PasswordIterations)
    VALUES (NEWID(), N'analista',
            0x32A670310607A7C3619BFD18A2A23F288E98008052F094920D2796ACAC8A2263,
            0xC6599FF57F29B58EA54AD4A56EAB2317,
            600000);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Surveys)
BEGIN
    DECLARE @now DATETIME2(0) = SYSUTCDATETIME();
    DECLARE @owner INT = (SELECT UserId FROM dbo.Users WHERE Username = N'admin');
    DECLARE @surveyId INT;

    INSERT INTO dbo.Surveys (ExternalId, PublicToken, OwnerUserId, Name, Description, CreatedAtUtc, UpdatedAtUtc)
    VALUES (NEWID(), NEWID(), @owner, N'Satisfacción del cliente 2026',
            N'Encuesta de ejemplo creada por el script de datos de demostración.', @now, @now);
    SET @surveyId = SCOPE_IDENTITY();

    INSERT INTO dbo.SurveyFields (SurveyId, Name, Title, FieldTypeId, IsRequired, SortOrder) VALUES
        (@surveyId, 'nombre',        N'¿Cuál es tu nombre?',                1, 1, 0),
        (@surveyId, 'calificacion',  N'Calificación del servicio (1 a 10)', 2, 1, 1),
        (@surveyId, 'fecha_visita',  N'¿Cuándo nos visitaste?',             3, 0, 2),
        (@surveyId, 'comentarios',   N'Comentarios adicionales',            1, 0, 3);
END
GO
