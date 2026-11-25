-- Crea la base de datos. RCSI: las lecturas no bloquean ni son bloqueadas por escrituras.

IF DB_ID(N'AcmeSurveys') IS NULL
BEGIN
    CREATE DATABASE AcmeSurveys COLLATE Latin1_General_100_CI_AS_SC;
END
GO

ALTER DATABASE AcmeSurveys SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
