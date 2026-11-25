# Acme Surveys

Plataforma de encuestas dinámicas en **ASP.NET MVC 5 (.NET Framework 4.8) y SQL Server**. Prueba técnica para
Devel Systems.

## Requerimientos cubiertos

| # | Requerimiento | Implementación |
|---|---|---|
| 1 | Login con usuario y contraseña | `/account/login`: cookie OWIN, PBKDF2-SHA256, bloqueo de cuenta y *rate limit* |
| 2 | CRUD de encuestas con campos dinámicos (nombre, título, requerido, tipo Texto/Número/Fecha) y link único | `/surveys`: editor para agregar, reordenar y quitar campos; link público `/s/{uuid}` regenerable |
| 3 | Llenado público sin login, guardando según el tipo | `/s/{token}`: validación y normalización por tipo (Strategy) y cifrado de cada valor |
| 4 | Pantalla de resultados | `/surveys/{id}/results`: tabla paginada que descifra solo la página visible |
| + | Extras | API REST `/api/v1` con JWT (CRUD, llenado y resultados), Swagger UI, *rate limit*, bitácora de seguridad y 67 pruebas unitarias |

## Stack

| Capa | Tecnología |
|---|---|
| Web | ASP.NET MVC 5.3, Web API 2, OWIN/Katana 4.2, Razor, Bootstrap 5.3 (local) |
| DI | Autofac 4.9 |
| Datos | SQL Server (LocalDB en desarrollo), Dapper 2.1, scripts SQL versionados |
| Seguridad | PBKDF2-HMAC-SHA256, AES-256-CBC + HMAC-SHA256, JWT HS256 |
| Documentación de la API | Swashbuckle.Core 5.6 (OpenAPI) y swagger-ui 5.34 local |
| Pruebas | xUnit 2.9 |

## Puesta en marcha

**Requisitos:** Windows 10/11 y Visual Studio 2022 con la carga *ASP.NET y desarrollo web*. Esa carga incluye
.NET Framework 4.8, IIS Express y SQL Server LocalDB.

```powershell
git clone https://github.com/xsiliezxr/acme_surveys.git
cd acme_surveys

# 1. Secretos locales: claves aleatorias de 256 bits (el archivo queda fuera de git)
powershell -ExecutionPolicy Bypass -File tools\New-DevSecrets.ps1

# 2. Base de datos en LocalDB: esquema y datos de demostración
powershell -ExecutionPolicy Bypass -File tools\Initialize-Database.ps1

# 3a. Visual Studio: abrir AcmeSurveys.sln y presionar F5

# 3b. Terminal (PowerShell estándar): ubica MSBuild automáticamente, compila y levanta IIS Express

& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" AcmeSurveys.sln -restore
powershell -ExecutionPolicy Bypass -File tools\Start-WebApp.ps1

# 3c. Si el comando anterior lanza error es porque no encuentra el MSBuild.exe
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
& $msbuild AcmeSurveys.sln -restore
```

| URL | Contenido |
|---|---|
| `https://localhost:44385/account/login` | Login (punto de entrada) |
| `https://localhost:44385/` | Aplicación (sin sesión redirige al login) |
| `http://localhost:5080/` | HTTP: redirige a HTTPS |
| `https://localhost:44385/swagger` | Swagger UI (solo en desarrollo) |
| `https://localhost:44385/api/v1/...` | API REST (rutas en [docs/API.md](docs/API.md)) |

`Start-WebApp.ps1` y la ventana *Salida* de Visual Studio muestran estas URLs al arrancar. El puerto HTTP es 5080
porque 8080 suele estar ocupado (Docker, otros servidores).

La primera ejecución pide confiar en el certificado de desarrollo de IIS Express. Para otra instancia de SQL Server
se usa `Initialize-Database.ps1 -Server ".\SQLEXPRESS"` y se ajusta la *connection string* en
`src/AcmeSurveys.Web/Web.config`.

| Usuario | Contraseña | Uso |
|---|---|---|
| `admin` | `Acme#Admin2026!` | Dueño de la encuesta de ejemplo |
| `analista` | `Acme#Analista2026!` | Comprueba que un usuario no accede a encuestas ajenas |

## Administrar la base de datos

La BD `AcmeSurveys` vive en LocalDB, instancia `(localdb)\MSSQLLocalDB`.

| Herramienta | Cómo conectarse |
|---|---|
| Visual Studio | *Ver › Explorador de objetos de SQL Server* → *Agregar SQL Server* → `(localdb)\MSSQLLocalDB` |
| SQL Server Management Studio | Servidor `(localdb)\MSSQLLocalDB`, autenticación de Windows |
| Terminal | `sqlcmd -S "(localdb)\MSSQLLocalDB" -d AcmeSurveys -Q "SELECT Name FROM dbo.Surveys"` |

Para volver al estado inicial se ejecuta `tools\Initialize-Database.ps1 -Recreate` (borra los datos). Los valores de
`dbo.ResponseValues` se ven como binario porque se cifran en la aplicación.

## Pruebas

### Unitarias

```powershell
dotnet test tests\AcmeSurveys.UnitTests
```

- 67 pruebas sobre dominio, casos de uso y criptografía. No necesitan BD ni servidor web: usan dobles en memoria
  (`TestDoubles/`).
- Nombres con el formato `Método_Escenario_ResultadoEsperado`, estructura Arrange/Act/Assert y un comportamiento por
  prueba.
- Una clase de prueba por componente, en la misma carpeta de capa que el código probado.

### Manual (10 minutos)

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | Iniciar sesión con `admin` | Lista "Mis encuestas" con la encuesta de ejemplo |
| 2 | Crear una encuesta con un campo de cada tipo, uno opcional, y reordenarlos | Se guarda en el orden mostrado y genera un link público |
| 3 | Abrir el link en una ventana privada y enviar texto en el campo numérico o dejar vacío un requerido | Errores junto a cada campo; no se guarda nada |
| 4 | Enviar valores válidos (incluido `<script>alert(1)</script>` en un texto) | Pantalla de agradecimiento |
| 5 | Ver *Resultados* | Valores descifrados y tipados; el script se muestra como texto |
| 6 | `SELECT * FROM dbo.ResponseValues` en LocalDB | Solo binario (`0x0101…`) |
| 7 | Editar una encuesta con respuestas e intentar cambiar el tipo o quitar un campo | Rechazado con mensaje |
| 8 | *Regenerar link* | El link anterior responde 404 |
| 9 | Iniciar sesión con `analista` y abrir la URL de edición de una encuesta de `admin` | 404 |
| 10 | Fallar el login de `admin` 5 veces y luego usar la contraseña correcta | Mismo mensaje genérico; cuenta bloqueada 15 min |
| 11 | Abrir *Editar* de la misma encuesta en dos pestañas, guardar en la primera y luego en la segunda | La segunda muestra "La encuesta fue modificada en otra pestaña…" (`ROWVERSION`) |
| 12 | Swagger: `POST /api/v1/auth/token` → *Authorize* con `Bearer {token}` → `GET /api/v1/surveys` | 200 con las encuestas del usuario |
| 13 | Swagger: `POST /api/v1/surveys`, luego `PUT` dos veces con el mismo `rowVersion` | `201`, `200` y `409 conflict` |
| 14 | Swagger: `POST /api/v1/public/surveys/{token}/responses` sin token JWT | `201`; la respuesta aparece en *Resultados* |
| 15 | `curl -I http://localhost:5080/account/login` | `301` hacia `https://localhost:44385/account/login` |

`ROWVERSION` protege la **edición** de una encuesta. Abrir el formulario público en dos pestañas y enviar ambas
registra dos respuestas, que es lo esperado: el llenado es anónimo y cada envío es una respuesta nueva.

## Estructura

```
├── database/                      Scripts SQL: 00 crear BD · 01 esquema · 02 datos demo · 03 login de mínimo privilegio
├── docs/                          Arquitectura, base de datos, seguridad y API
├── src/
│   ├── AcmeSurveys.Domain/          Entidades, reglas y handlers por tipo de campo (sin dependencias)
│   ├── AcmeSurveys.Application/     Casos de uso y puertos (interfaces)
│   ├── AcmeSurveys.Infrastructure/  Dapper, PBKDF2, AES+HMAC, JWT y bitácora
│   └── AcmeSurveys.Web/             MVC, API, vistas, seguridad HTTP y composition root
├── tests/AcmeSurveys.UnitTests/   Pruebas xUnit
└── tools/                         Scripts de entorno: secretos, BD e IIS Express
```

## Decisiones clave

- **Clean Architecture en 4 proyectos.** El compilador hace cumplir la regla de dependencia: Domain no referencia
  SQL, HTTP ni criptografía.
- **BD en 3FN y fila por valor** (`ResponseValues`). Los campos se definen en tiempo de ejecución sin DDL dinámico
  ni JSON, con FK reales por campo.
- **IDENTITY interno y UUID v4 externo.** PK compactas para índices y *joins*; UUID no enumerable en URLs, en el link
  público (rotable) y en el `sub` del JWT.
- **Código dinámico con Strategy.** Cada tipo de campo implementa `IFieldValueHandler`; agregar un tipo es una clase
  y una fila de catálogo.
- **Contraseñas con PBKDF2-HMAC-SHA256** (600 000 iteraciones, sal por usuario, *rehash* transparente).
- **Respuestas cifradas en la aplicación** con AES-256-CBC + HMAC-SHA256 (Encrypt-then-MAC) y rotación de claves.
  La clave nunca llega a SQL Server.
- **Lectura cifrada optimizada:** paginación en SQL, un viaje por página, claves derivadas una vez y pivote O(R+V).
- **Cookie para la web y JWT para la API.** Cada mecanismo se usa donde es más seguro.
- **Seguro por defecto:** `[Authorize]`, anti-CSRF y HTTPS globales (HTTP redirige); CSP estricta; *rate limit*;
  errores sin trazas.
- **Concurrencia optimista con `ROWVERSION`:** si dos sesiones editan la misma encuesta, la segunda en guardar recibe
  un aviso en lugar de pisar los cambios.

Detalle y alternativas descartadas en [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md),
[docs/BASE_DE_DATOS.md](docs/BASE_DE_DATOS.md), [docs/SEGURIDAD.md](docs/SEGURIDAD.md) y [docs/API.md](docs/API.md).

## Uso de IA

Se usó un asistente de IA como apoyo de programación. El diseño, la revisión y las pruebas de punta a punta fueron
deliberados, y cada decisión está justificada en `docs/`.
