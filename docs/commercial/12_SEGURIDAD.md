# 12 · Seguridad y multitenancy

## Modelo de identidades

- **User** — empleado interno (vendedor, almacén, supervisor).
- **B2BUser** — usuario externo del cliente, con acceso a su `B2BAccount`.
- **PortalUser** — alias o entidad unificada con `B2BUser` para portal.
- **ApiClient** — clientes de integraciones (otros sistemas, webhooks).
- **SystemUser** — para jobs internos.

## JWT — anatomía

Access token (15 min):

```json
{
  "sub": "u_1837",
  "company_id": "c_42",
  "tenant": "acme",
  "type": "internal",
  "roles": ["seller", "crm_user"],
  "scopes": ["orders:write", "crm:read"],
  "iat": 1747843200,
  "exp": 1747844100,
  "jti": "01H..."
}
```

Refresh token (30 días, rotatorio):

- Almacenado opaco en BD (`RefreshToken { id, userId, hash, expiresAt, revokedAt, replacedBy }`).
- Cada rotación invalida el anterior.
- Reuse detection: si llega un refresh ya rotado, **se revoca toda la familia** y se obliga a iniciar sesión.

## Multitenancy — capas defensivas

1. **JWT claim `company_id`** — primera línea.
2. **Tenant middleware** — fija `ITenantContext.CompanyId` y rechaza requests sin claim válido.
3. **EF global query filter** — `e.CompanyId == _tenant.CompanyId`.
4. **Postgres RLS** — política sobre cada tabla:

   ```sql
   ALTER TABLE sales_order ENABLE ROW LEVEL SECURITY;
   CREATE POLICY tenant_iso_sales_order ON sales_order
     USING (company_id = current_setting('app.company_id', true)::bigint);
   ```

5. **Auditoría** — cada query log incluye `company_id`; alertas si un usuario hace peticiones sobre otra empresa.

### Cambio de empresa (multi-acceso)

- `POST /auth/switch-company` exige refresh válido + verificación de membership.
- Emite nuevo access token con `company_id` distinto.
- App móvil pide confirmación y limpia caches dependientes.

## Roles y permisos

- Roles base preconfigurados; cada empresa puede crear roles personalizados.
- Matriz `Role × Module × Action` en `RolePermission(RoleId, Module, Action)`.

Ejemplos de actions: `customers.read`, `customers.write`, `orders.cancel`, `prices.edit`, `dashboards.exec`, `cfdi.stamp`.

- `[Authorize(Policy = "orders.write")]` en endpoints.
- `PermissionService.Has(userId, action, scopeContext)` para chequeos contextuales (p. ej. vendedor solo edita pedidos de sus clientes).

## MFA

- **TOTP** (Google Authenticator) por default.
- **WebAuthn / passkeys** en fase 2.
- **SMS** opcional (Twilio) para usuarios no tech.
- Obligatorio para roles con permisos críticos (`cfdi.stamp`, `payments.execute`).

## Login B2B

- Email + password.
- Soporte para SSO empresarial vía OIDC opcional.
- Invitación con token caducable.
- Bloqueo después de 5 intentos fallidos por 15 min.
- Notificación al titular del `B2BAccount` ante intentos sospechosos.

## Protección de datos

- **Cifrado en reposo:** PostgreSQL con cifrado a nivel volumen + columnas sensibles (RFC, CURP, teléfonos) con `pgcrypto`.
- **Cifrado en tránsito:** TLS 1.3 en todo el stack.
- **Backups cifrados** diarios, con prueba mensual de restauración.
- **Retención:** datos personales borrados a solicitud (LFPDPPP / GDPR-like).
- **Logs scrubbed:** PII enmascarada en logs (RFC, emails parcialmente).

## CFDI 4.0 — seguridad fiscal

- Certificados (.cer/.key) por empresa, encriptados con KMS local.
- Acceso únicamente desde `ICfdiSigner` con auditoría.
- Doble validación previa al timbrado: estructura + reglas de negocio + simulación PAC sandbox.
- Rate limit estricto en endpoints de timbrado.

## Rate limiting

- Por usuario: 600 req/min default; endpoints sensibles 60 req/min.
- Por IP: 1500 req/min anónimo, 6000 autenticado.
- Headers `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset`.
- 429 con `Retry-After`.

## CSRF / CORS

- Blazor Server: protección CSRF nativa (`Antiforgery`).
- API REST: CSRF irrelevante por uso de JWT en `Authorization`.
- CORS estricto por dominios autorizados por empresa.

## Auditoría

- `AuditLog` ya existente registra eventos críticos: login, logout, cambio de rol, modificación de precio, cancelación de pedido, timbrado, eliminación.
- Visualizador con filtros por usuario, módulo, fecha.
- Retención mínimo 1 año, recomendado 3 años.

## Webhooks

- Endpoints firmados con HMAC SHA-256 (`X-Nanchesoft-Signature`).
- `whsec` por cliente, rotable.
- Reintentos exponenciales hasta 24 h.
- Cola por cliente para no interferir entre suscriptores.

## Móvil

- Almacenamiento de tokens en Keychain/Keystore con biometría como gate.
- `flutter_jailbreak_detection` opcional para bloquear dispositivos rooted/jailbroken si la empresa lo configura.
- PIN local de 6 dígitos opcional.
- Wipe remoto: dispositivo perdido → admin invalida tokens y limpia BD local al primer arranque.

## DevSecOps

- **SAST**: GitHub CodeQL en cada PR.
- **Dependency scanning**: Dependabot + Snyk.
- **SBOM** generado por release.
- **Secrets** en Azure Key Vault / Hashicorp Vault, **nunca** en repo.
- Pre-commit hook `detect-secrets`.

## Cumplimiento

- **NOM-151** para integridad de documentos electrónicos (timestamping).
- **LFPDPPP** — aviso de privacidad, consentimiento explícito, derechos ARCO.
- **SAT** — anexos del Anexo 20 v4.0.
- **ISO 27001** — roadmap a 18 meses (controles, no certificación obligatoria).
