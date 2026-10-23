import { CanActivate, ExecutionContext, ForbiddenException, Injectable, SetMetadata, UnauthorizedException } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { JwtService } from '@nestjs/jwt';
import { cfg } from './config';

export interface Principal { sub: string; name: string; email: string; role: string; permissions: string[]; }

export const Public = () => SetMetadata('public', true);
export const Roles = (...roles: string[]) => SetMetadata('roles', roles);

/** Valida Issuer, Audience, firma y expiración; luego aplica @Roles. Global (APP_GUARD). */
@Injectable()
export class JwtAuthGuard implements CanActivate {
  constructor(private jwt: JwtService, private reflector: Reflector) {}

  async canActivate(ctx: ExecutionContext): Promise<boolean> {
    const targets = [ctx.getHandler(), ctx.getClass()];
    if (this.reflector.getAllAndOverride<boolean>('public', targets)) return true;
    const req = ctx.switchToHttp().getRequest();
    const header: string = req.headers['authorization'] ?? '';
    if (!header.startsWith('Bearer ')) throw new UnauthorizedException({ mensaje: 'Autenticación requerida.' });
    try {
      const p = await this.jwt.verifyAsync(header.slice(7), {
        secret: cfg.jwtKey, issuer: cfg.issuer, audience: cfg.audience, algorithms: ['HS256'],
      });
      req.user = p as Principal;
    } catch {
      throw new UnauthorizedException({ mensaje: 'Token inválido o expirado.' });
    }
    const roles = this.reflector.getAllAndOverride<string[]>('roles', targets);
    if (roles?.length && !roles.includes(req.user.role)) {
      throw new ForbiddenException({ mensaje: 'No tiene permisos para esta operación.' });
    }
    return true;
  }
}
