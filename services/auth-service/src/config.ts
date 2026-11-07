export const cfg = {
  jwtKey: process.env.JWT_KEY ?? '',
  issuer: process.env.JWT_ISSUER ?? 'AlertaClimatica.Auth',
  audience: process.env.JWT_AUDIENCE ?? 'AlertaClimatica.Client',
  expirationMinutes: Number(process.env.JWT_EXPIRATION_MINUTES ?? 15),   // token de acceso corto
  refreshDays: Number(process.env.REFRESH_TOKEN_DAYS ?? 7),
  maxFailedLogins: Number(process.env.MAX_FAILED_LOGINS ?? 5),
  lockMinutes: Number(process.env.LOCK_MINUTES ?? 15),
  auditUrl: (process.env.AUDIT_SERVICE_URL ?? 'http://audit-service:8000').replace(/\/$/, ''),
  oauth: {
    /** URL pública del sitio (la misma que ve el navegador). De ella salen los redirect URI registrados en Google/GitHub. */
    publicUrl: (process.env.PUBLIC_URL ?? 'http://localhost:4200').replace(/\/$/, ''),
    /** false (por defecto): una cuenta nueva creada por Google/GitHub queda pendiente de aprobación, igual que el registro público. */
    autoActivate: (process.env.OAUTH_AUTO_ACTIVATE ?? 'false') === 'true',
    stateMinutes: 10,
    // RNF-OAUTH-01/02: client_id y client_secret SOLO por variables de entorno / Secret de Kubernetes; nunca en código ni en el frontend.
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID ?? '',
      clientSecret: process.env.GOOGLE_CLIENT_SECRET ?? '',
      discoveryUrl: process.env.GOOGLE_DISCOVERY_URL ?? 'https://accounts.google.com/.well-known/openid-configuration',
    },
    github: {
      clientId: process.env.GITHUB_CLIENT_ID ?? '',
      clientSecret: process.env.GITHUB_CLIENT_SECRET ?? '',
      authorizeUrl: process.env.GITHUB_AUTHORIZE_URL ?? 'https://github.com/login/oauth/authorize',
      tokenUrl: process.env.GITHUB_TOKEN_URL ?? 'https://github.com/login/oauth/access_token',
      apiUrl: (process.env.GITHUB_API_URL ?? 'https://api.github.com').replace(/\/$/, ''),
    },
  },
};

export function validarConfig() {
  if (Buffer.byteLength(cfg.jwtKey) < 32) {
    throw new Error('JWT_KEY debe tener al menos 32 caracteres (defínala por variable de entorno / Secret).');
  }
  // Un proveedor OAuth a medias (id sin secreto o al revés) es un error de despliegue: se detecta al iniciar.
  for (const [nombre, p] of [['GOOGLE', cfg.oauth.google], ['GITHUB', cfg.oauth.github]] as const) {
    if (!!p.clientId !== !!p.clientSecret) throw new Error(`${nombre}_CLIENT_ID y ${nombre}_CLIENT_SECRET deben definirse juntos (o ninguno para deshabilitar ese proveedor).`);
  }
}
