-- Esquema en 3FN. PK internas INT/BIGINT IDENTITY (clustered); ids expuestos UNIQUEIDENTIFIER (UUID v4).
-- Justificación completa en docs/BASE_DE_DATOS.md.

USE AcmeSurveys;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    RAISERROR(N'El esquema ya existe. Use tools/Initialize-Database.ps1 -Recreate para recrearlo.', 16, 1);
    SET NOEXEC ON;
END
GO

CREATE TABLE dbo.Users
(
    UserId              INT IDENTITY(1,1)  NOT NULL,
    ExternalId          UNIQUEIDENTIFIER   NOT NULL,
    Username            NVARCHAR(50)       NOT NULL,
    PasswordHash        BINARY(32)         NOT NULL,           -- PBKDF2-HMAC-SHA256
    PasswordSalt        BINARY(16)         NOT NULL,
    PasswordIterations  INT                NOT NULL,           -- por fila: permite subir el costo y re-hashear
    FailedLoginCount    TINYINT            NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0),
    LockoutEndUtc       DATETIME2(0)       NULL,
    LastLoginAtUtc      DATETIME2(0)       NULL,
    CreatedAtUtc        DATETIME2(0)       NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Users              PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_ExternalId   UNIQUE (ExternalId),
    CONSTRAINT UQ_Users_Username     UNIQUE (Username),
    CONSTRAINT CK_Users_Username     CHECK (LEN(Username) >= 3),
    CONSTRAINT CK_Users_Iterations   CHECK (PasswordIterations >= 100000)
);
GO

CREATE TABLE dbo.FieldTypes
(
    FieldTypeId  TINYINT      NOT NULL,
    Code         VARCHAR(10)  NOT NULL,
    DisplayName  NVARCHAR(30) NOT NULL,

    CONSTRAINT PK_FieldTypes      PRIMARY KEY CLUSTERED (FieldTypeId),
    CONSTRAINT UQ_FieldTypes_Code UNIQUE (Code)
);
GO

INSERT INTO dbo.FieldTypes (FieldTypeId, Code, DisplayName) VALUES
    (1, 'TEXT',   N'Texto'),
    (2, 'NUMBER', N'Número'),
    (3, 'DATE',   N'Fecha');
GO

CREATE TABLE dbo.Surveys
(
    SurveyId      INT IDENTITY(1,1) NOT NULL,
    ExternalId    UNIQUEIDENTIFIER  NOT NULL,
    PublicToken   UNIQUEIDENTIFIER  NOT NULL,                  -- link público, distinto de ExternalId y rotable
    OwnerUserId   INT               NOT NULL,
    Name          NVARCHAR(150)     NOT NULL,
    Description   NVARCHAR(1000)    NULL,
    CreatedAtUtc  DATETIME2(0)      NOT NULL,
    UpdatedAtUtc  DATETIME2(0)      NOT NULL,
    DeletedAtUtc  DATETIME2(0)      NULL,                      -- soft delete: conserva los resultados
    RowVersion    ROWVERSION        NOT NULL,                  -- concurrencia optimista

    CONSTRAINT PK_Surveys             PRIMARY KEY CLUSTERED (SurveyId),
    CONSTRAINT UQ_Surveys_ExternalId  UNIQUE (ExternalId),
    CONSTRAINT UQ_Surveys_PublicToken UNIQUE (PublicToken),
    CONSTRAINT FK_Surveys_Users       FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Surveys_Name        CHECK (LEN(Name) >= 3),
    CONSTRAINT CK_Surveys_Dates       CHECK (UpdatedAtUtc >= CreatedAtUtc)
);
GO

CREATE NONCLUSTERED INDEX IX_Surveys_Owner_Active
    ON dbo.Surveys (OwnerUserId, CreatedAtUtc DESC)
    INCLUDE (ExternalId, PublicToken, Name, Description, UpdatedAtUtc)
    WHERE DeletedAtUtc IS NULL;
GO

CREATE TABLE dbo.SurveyFields
(
    SurveyFieldId  INT IDENTITY(1,1) NOT NULL,
    SurveyId       INT               NOT NULL,
    Name           VARCHAR(50)       NOT NULL,
    Title          NVARCHAR(200)     NOT NULL,
    FieldTypeId    TINYINT           NOT NULL,
    IsRequired     BIT               NOT NULL,
    SortOrder      SMALLINT          NOT NULL,

    CONSTRAINT PK_SurveyFields              PRIMARY KEY CLUSTERED (SurveyFieldId),
    CONSTRAINT FK_SurveyFields_Surveys      FOREIGN KEY (SurveyId)    REFERENCES dbo.Surveys (SurveyId),
    CONSTRAINT FK_SurveyFields_FieldTypes   FOREIGN KEY (FieldTypeId) REFERENCES dbo.FieldTypes (FieldTypeId),
    CONSTRAINT UQ_SurveyFields_Survey_Name  UNIQUE (SurveyId, Name),
    CONSTRAINT CK_SurveyFields_Name         CHECK (Name LIKE '[a-zA-Z]%' AND Name NOT LIKE '%[^a-zA-Z0-9_]%'),
    CONSTRAINT CK_SurveyFields_SortOrder    CHECK (SortOrder >= 0)
);
GO

CREATE TABLE dbo.Responses
(
    ResponseId      BIGINT IDENTITY(1,1) NOT NULL,
    SurveyId        INT                  NOT NULL,
    SubmittedAtUtc  DATETIME2(0)         NOT NULL CONSTRAINT DF_Responses_SubmittedAtUtc DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Responses          PRIMARY KEY CLUSTERED (ResponseId),
    CONSTRAINT FK_Responses_Surveys  FOREIGN KEY (SurveyId) REFERENCES dbo.Surveys (SurveyId)
);
GO

CREATE NONCLUSTERED INDEX IX_Responses_Survey
    ON dbo.Responses (SurveyId, ResponseId DESC)
    INCLUDE (SubmittedAtUtc);
GO

CREATE TABLE dbo.ResponseValues
(
    ResponseId     BIGINT          NOT NULL,
    SurveyFieldId  INT             NOT NULL,
    ValueCipher    VARBINARY(4000) NOT NULL,                   -- AES-256-CBC + HMAC-SHA256; 66 bytes = tamaño mínimo

    CONSTRAINT PK_ResponseValues               PRIMARY KEY CLUSTERED (ResponseId, SurveyFieldId),
    CONSTRAINT FK_ResponseValues_Responses     FOREIGN KEY (ResponseId)    REFERENCES dbo.Responses (ResponseId) ON DELETE CASCADE,
    CONSTRAINT FK_ResponseValues_SurveyFields  FOREIGN KEY (SurveyFieldId) REFERENCES dbo.SurveyFields (SurveyFieldId),
    CONSTRAINT CK_ResponseValues_MinLength     CHECK (DATALENGTH(ValueCipher) >= 66)
);
GO

CREATE NONCLUSTERED INDEX IX_ResponseValues_SurveyField
    ON dbo.ResponseValues (SurveyFieldId);
GO

CREATE TYPE dbo.SurveyFieldTableType AS TABLE
(
    SurveyFieldId  INT           NULL,
    Name           VARCHAR(50)   NOT NULL,
    Title          NVARCHAR(200) NOT NULL,
    FieldTypeId    TINYINT       NOT NULL,
    IsRequired     BIT           NOT NULL,
    SortOrder      SMALLINT      NOT NULL PRIMARY KEY
);
GO

CREATE TYPE dbo.ResponseValueTableType AS TABLE
(
    SurveyFieldId  INT             NOT NULL PRIMARY KEY,
    ValueCipher    VARBINARY(4000) NOT NULL
);
GO

SET NOEXEC OFF;
GO
