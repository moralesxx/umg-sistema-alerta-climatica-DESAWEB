import { Injectable, Logger } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { cfg } from './config';
import { Principal } from './security';

/** Envía registros al Audit Service por API (mejor esfuerzo; no bloquea la petición). */
@Injectable()
export class AuditClient {
  private log = new Logger('Audit');
  constructor(private jwt: JwtService) {}

  registrar(user: Partial<Principal> | null, accion: string, detalle: string, entidad?: string, entidadId?: number, usuarioId?: number) {
    const token = this.jwt.sign(
      { sub: 'svc:auth-service', role: 'Servicio' },
      { secret: cfg.jwtKey, issuer: cfg.issuer, audience: cfg.audience, expiresIn: 120 },
    );
    const id = usuarioId ?? (user?.sub ? Number(user.sub) : undefined);
    fetch(`${cfg.auditUrl}/api/audit/internal`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ usuarioId: id, usuarioNombre: user?.name, accion, detalle, entidad, entidadId, servicio: 'auth-service' }),
      signal: AbortSignal.timeout(5000),
    }).catch((e) => this.log.warn(`No se pudo registrar auditoría: ${e.message}`));
  }
}
