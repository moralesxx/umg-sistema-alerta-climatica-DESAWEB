export const cfg = {
  jwtKey: process.env.JWT_KEY ?? '',
  issuer: process.env.JWT_ISSUER ?? 'AlertaClimatica.Auth',
  audience: process.env.JWT_AUDIENCE ?? 'AlertaClimatica.Client',
  expirationMinutes: Number(process.env.JWT_EXPIRATION_MINUTES ?? 60),
  auditUrl: (process.env.AUDIT_SERVICE_URL ?? 'http://audit-service:8000').replace(/\/$/, ''),
};

export function validarConfig() {
  if (Buffer.byteLength(cfg.jwtKey) < 32) {
    throw new Error('JWT_KEY debe tener al menos 32 caracteres (defínala por variable de entorno / Secret).');
  }
}
