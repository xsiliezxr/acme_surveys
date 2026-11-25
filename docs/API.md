# API REST `/api/v1`

API para integraciones: gestión de encuestas, llenado público y lectura de resultados. Reutiliza los mismos casos de
uso de Application que la web, con las mismas reglas, verificación de dueño y control de concurrencia. Exige HTTPS;
los endpoints de gestión exigen además JWT.

- JSON en `camelCase`. Fechas con hora en UTC ISO-8601 y campos de tipo fecha como `yyyy-MM-dd`.
- Errores de negocio: `{"error": "<código>", "message": "...", "errors": [{"target": "...", "message": "..."}]}`.
  `errors` solo aparece en `validation` e indica el dato con problema (`name`, `fields[0].type`, `values.edad`).
- Códigos de negocio: `validation` (400), `not_found` (404), `conflict` (409) e `invalid_credentials` (401).
- Errores de infraestructura: `401` sin token o con token inválido (cabecera `WWW-Authenticate: Bearer`,
  `error="invalid_token"`), `403 {"error":"https_required"}` por HTTP y `429 {"error":"rate_limited",
  "retryAfterSeconds": N}` con cabecera `Retry-After`.
- Un recurso ajeno o inexistente responde 404.

## Swagger

| URL | Contenido |
|---|---|
| `/swagger` | Swagger UI |
| `/swagger/docs/v1` | Documento OpenAPI 2.0 |

Swagger solo está activo en desarrollo: `Web.Release.config` pone `Acme:Swagger:Enabled=false`. La UI es
swagger-ui 5 servida localmente, cumple la CSP y no guarda el JWT en `localStorage`.

Prueba rápida: `POST /api/v1/auth/token` → copiar `accessToken` → *Authorize* con `Bearer {token}` →
`GET /api/v1/surveys`.

## Endpoints

| Método | Ruta | Auth | Respuesta |
|---|---|---|---|
| `POST` | `/api/v1/auth/token` | No | `200` token · `400` · `401 invalid_credentials` |
| `GET` | `/api/v1/surveys` | JWT | Encuestas activas del usuario |
| `POST` | `/api/v1/surveys` | JWT | `201` + `Location` con la encuesta creada · `400` |
| `GET` | `/api/v1/surveys/{id}` | JWT | Encuesta con sus campos y su `rowVersion` |
| `PUT` | `/api/v1/surveys/{id}` | JWT | `200` con el nuevo `rowVersion` · `400` · `404` · `409 conflict` |
| `DELETE` | `/api/v1/surveys/{id}` | JWT | `204` (borrado lógico) · `404` |
| `POST` | `/api/v1/surveys/{id}/public-link` | JWT | `200` con la nueva `publicUrl`; la anterior deja de funcionar |
| `GET` | `/api/v1/surveys/{id}/results?page=1&pageSize=25` | JWT | Respuestas descifradas, tipadas y paginadas (`pageSize` hasta 100; 0 o negativo usa 25) |
| `GET` | `/api/v1/public/surveys/{token}` | No | Formulario de la encuesta · `404` |
| `POST` | `/api/v1/public/surveys/{token}/responses` | No | `201` · `400` por campo · `404` · `409` si la encuesta cambió |

*Rate limit* por IP: token 5/min, envíos públicos 10/min, toda la API 60/min y global 300/min.

## Edición y concurrencia

`PUT` reemplaza la definición completa: los campos que no se envían se eliminan. El campo existente se reconoce por
su `name`, así que cambiar el nombre equivale a eliminarlo y crear otro. Con respuestas registradas no se puede
cambiar el tipo ni eliminar campos (`400`).

El cuerpo lleva el `rowVersion` del último `GET`. Si otra sesión guardó antes, la respuesta es `409` y hay que leer de
nuevo la encuesta. Es la misma protección `ROWVERSION` que usa el formulario web.

## Ejemplos

```bash
API=https://localhost:44385/api/v1
TOKEN=$(curl -s $API/auth/token -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Acme#Admin2026!"}' | jq -r .accessToken)

# Crear
curl -s $API/surveys -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "name": "Clima laboral",
  "fields": [
    { "name": "edad", "title": "Edad", "type": "number", "isRequired": true },
    { "name": "ingreso", "title": "Fecha de ingreso", "type": "date" }
  ]}'

# Editar (rowVersion del GET o del POST anterior)
curl -s -X PUT $API/surveys/{id} -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "name": "Clima laboral 2026", "rowVersion": "AAAAAAAAw1E=",
  "fields": [ { "name": "edad", "title": "Edad", "type": "number", "isRequired": true } ]}'

# Responder sin login (token del link público /s/{token})
curl -s $API/public/surveys/{token}/responses -H "Content-Type: application/json" \
  -d '{"values": {"edad": "34", "ingreso": "2024-03-01"}}'
```

Los números usan punto decimal. Los campos opcionales se pueden omitir en `values`.

## Token

El token dura 15 minutos y no tiene *refresh token*. Los claims son `sub` (UUID externo del usuario), `name`, `jti`,
`iat`, `nbf`, `exp`, `iss` y `aud`.

En los resultados, `hasIntegrityError: true` indica que un valor no pasó la verificación HMAC; ese valor llega como
`null` y el evento queda en la bitácora.
