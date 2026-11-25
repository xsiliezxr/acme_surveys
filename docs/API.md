# API REST `/api/v1`

API de solo lectura para integraciones (por ejemplo, un tablero de BI). Reutiliza los mismos servicios de
Application que la web. Exige HTTPS y JWT, salvo en la emisión del token.

- JSON en `camelCase`. Fechas con hora en UTC ISO-8601 y campos de tipo fecha como `yyyy-MM-dd`.
- Errores: `{"error": "<código>", "message": "..."}` con los códigos `validation`, `not_found`, `conflict`,
  `invalid_credentials`, `rate_limited` y `https_required`.
- Un recurso ajeno o inexistente responde 404.

## Swagger

| URL | Contenido |
|---|---|
| `/swagger` | Swagger UI |
| `/swagger/docs/v1` | Documento OpenAPI 2.0 |

Swagger solo está activo en desarrollo: `Web.Release.config` pone `Acme:Swagger:Enabled=false`. La UI es
swagger-ui 5 servida localmente, cumple la CSP y no guarda el JWT en `localStorage`.

Prueba rápida en Swagger: `POST /api/v1/auth/token` → copiar `accessToken` → *Authorize* con `Bearer {token}` →
`GET /api/v1/surveys`.

## Endpoints

| Método | Ruta | Respuesta |
|---|---|---|
| `POST` | `/api/v1/auth/token` | `200` token · `401 invalid_credentials` · `429` |
| `GET` | `/api/v1/surveys` | Encuestas activas del usuario |
| `GET` | `/api/v1/surveys/{id}` | Encuesta con sus campos en orden |
| `GET` | `/api/v1/surveys/{id}/results?page=1&pageSize=25` | Respuestas descifradas, tipadas y paginadas (`pageSize` de 1 a 100) |

```bash
TOKEN=$(curl -s https://localhost:44385/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Acme#Admin2026!"}' | jq -r .accessToken)

curl -s https://localhost:44385/api/v1/surveys -H "Authorization: Bearer $TOKEN"
```

El token dura 15 minutos y no tiene *refresh token*. Los claims son `sub` (UUID externo del usuario), `name`, `jti`,
`iat`, `nbf`, `exp`, `iss` y `aud`.

En los resultados, `hasIntegrityError: true` indica que un valor no pasó la verificación HMAC; ese valor llega como
`null` y el evento queda en la bitácora.
