# Seguridad

## Contraseñas

Se guarda un hash lento con sal, nunca la contraseña ni un valor reversible.

| Parámetro | Valor |
|---|---|
| Algoritmo | PBKDF2-HMAC-SHA256 (`Rfc2898DeriveBytes`, NIST SP 800-132, sin dependencias externas) |
| Iteraciones | 600 000 (recomendación OWASP), guardadas por usuario |
| Sal | 16 bytes de CSPRNG por usuario |
| Comparación | Tiempo constante |

- **Rehash transparente:** si sube `Acme:PasswordHashIterations`, el hash se recalcula en el siguiente login.
- **Sin enumeración de usuarios:** usuario inexistente, contraseña errónea o cuenta bloqueada devuelven el mismo
  mensaje con el mismo costo de CPU.
- **Bloqueo:** 5 fallos bloquean 15 minutos. El contador se actualiza con un `UPDATE … OUTPUT` atómico.
- **Límites de entrada:** usuario de 50 caracteres y contraseña de 128 como máximo (evita DoS sobre PBKDF2).

## Cifrado de respuestas

Cada valor se cifra en la aplicación antes de llegar a la BD (`AesHmacFieldValueProtector`):

```
[versión 1][keyId 1][IV 16][AES-256-CBC][HMAC-SHA256 32]   MAC sobre versión‖keyId‖IV‖cifrado‖fieldId
```

| Decisión | Motivo |
|---|---|
| Encrypt-then-MAC | El MAC se verifica primero: evita *padding oracle* y maleabilidad de CBC |
| No AES-GCM | `AesGcm` no existe en .NET Framework 4.8 |
| Claves de cifrado y MAC derivadas por separado | Una clave nunca cumple dos funciones |
| IV aleatorio por valor | Respuestas iguales producen cifrados distintos |
| `fieldId` dentro del MAC | Un cifrado copiado a otro campo no valida |
| `keyId` en cada valor | Rotación sin migrar: se agrega `Key:2`, se cambia `ActiveKeyId` y lo anterior se sigue leyendo |
| Cifrado en la aplicación, no TDE | TDE solo protege archivos en disco; aquí un DBA, un backup o una inyección SQL solo ven binario |

Un valor que no pasa el MAC no se muestra: la fila se marca y se registra `CiphertextIntegrityFailure`.

**Optimización de lectura:** se descifra solo la página visible (paginación en SQL), en un único viaje a la BD,
con claves derivadas una vez y pivote en O(R + V).

**Secretos:** `secrets.config` queda fuera de git y se genera con `tools/New-DevSecrets.ps1`. La aplicación no
arranca si falta una clave o si no mide 256 bits. En producción vendrían de un gestor de secretos y la conexión
usaría el login de `database/03_least_privilege_login.sql`.

## Autenticación

| Canal | Mecanismo |
|---|---|
| Web | Cookie `__Host-AcmeAuth`: `HttpOnly`, `Secure`, `SameSite=Lax`, 30 min deslizantes y 8 h absolutas |
| API | JWT HS256 de 15 min en `Authorization: Bearer` |

El JWT valida firma, algoritmo exacto `HS256`, emisor, audiencia y expiración, con un tamaño máximo de 4 KB. En cada
petición se comprueba que el usuario siga activo.

## Rate limiting

*Sliding window counter* en un middleware OWIN. Responde 429 con `Retry-After`.

| Ruta | Límite por IP |
|---|---|
| `POST /account/login`, `POST /api/v1/auth/token` | 5 / min |
| `POST /s/*` | 10 / min |
| `/api/*` | 60 / min |
| Global | 300 / min |

El contador vive en memoria (por instancia); con varios servidores se movería a Redis o al WAF.

## OWASP Top 10:2025

| Riesgo | Controles |
|---|---|
| A01 Broken Access Control | `[Authorize]` global; verificación de dueño con 404 para recursos ajenos; UUID no enumerables |
| A02 Security Misconfiguration | `customErrors On`, sin cabeceras de versión, solo GET/HEAD/POST, CSP estricta, HSTS, sin CORS, Swagger apagado en Release |
| A03 Supply Chain | Versiones fijadas, NuGetAudit activo, librerías de cliente locales, dependencias mínimas |
| A04 Cryptographic Failures | PBKDF2, AES+HMAC, claves de 256 bits fuera del repositorio, TLS obligatorio |
| A05 Injection | SQL parametrizado, Razor codifica la salida, nombres de campo validados en dominio y en BD |
| A06 Insecure Design | Reglas en el dominio, concurrencia optimista, link rotable, ViewModels sin *mass assignment* |
| A07 Authentication Failures | Bloqueo, *rate limit*, mensaje genérico, timeouts, logout por POST, `ReturnUrl` solo local |
| A08 Data Integrity Failures | Anti-CSRF global, MAC por valor, TempData en cookie cifrada que solo admite cadenas |
| A09 Logging Failures | Bitácora de eventos de seguridad sin contraseñas, tokens ni respuestas; id de correlación en errores |
| A10 Exceptional Conditions | Result/Error para fallas esperadas, manejador global, transacciones con `XACT_ABORT` |
