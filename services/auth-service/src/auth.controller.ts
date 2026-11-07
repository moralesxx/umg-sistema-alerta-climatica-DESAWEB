import { Body, Controller, Get, HttpCode, Logger, NotFoundException, Param, ParseIntPipe, Patch, Post, Put, Query, Req, Res } from '@nestjs/common';
import { AuditClient } from './audit.client';
import { OAuthError, OAuthService } from './oauth.service';
import { Public, Roles } from './security';
import { UsersService } from './users.service';

@Controller()
export class HealthController {
  @Public() @Get('health') health() { return { estado: 'ok' }; }
}

@Controller('api/auth')
export class AuthController {
  constructor(private svc: UsersService) {}

  @Public() @Post('login') @HttpCode(200) login(@Body() b: any) { return this.svc.login(b); }
  @Public() @Post('register') async register(@Body() b: any) { return this.svc.registrarPublico(b); }
  @Public() @Post('refresh') @HttpCode(200) refresh(@Body() b: any) { return this.svc.refrescar(b?.refreshToken); }
  @Public() @Post('logout') @HttpCode(200)
  logout(@Body() b: any, @Req() r: any) {
    const h: string = r.headers['authorization'] ?? '';
    return this.svc.cerrarSesion(b?.refreshToken, h.startsWith('Bearer ') ? h.slice(7) : undefined);
  }

  /** Interno (rol Servicio): el gateway pregunta si la sesión del token sigue vigente. Nunca se expone hacia afuera. */
  @Roles('Servicio') @Get('internal/sessions/:sid')
  async sesion(@Param('sid') sid: string) {
    if (!(await this.svc.sesionActiva(sid))) throw new NotFoundException({ mensaje: 'Sesión inactiva.' });
    return { activa: true };
  }
  @Post('change-password') @HttpCode(200) cambiar(@Body() b: any, @Req() r: any) { return this.svc.cambiarContrasenia(r.user, b); }
  @Get('me') me(@Req() r: any) { return r.user; }

  @Roles('Administrador') @Get('roles') roles() { return this.svc.listarRoles(); }

  @Roles('Administrador') @Get('usuarios')
  listar(@Query('texto') texto?: string, @Query('rolId') rolId?: string, @Query('estado') estado?: string) {
    return this.svc.listar(texto, rolId ? Number(rolId) : undefined, estado === undefined ? undefined : estado === 'true');
  }
  @Roles('Administrador') @Post('usuarios/administracion') crear(@Body() b: any, @Req() r: any) { return this.svc.crearAdmin(b, r.user); }
  @Roles('Administrador') @Get('usuarios/:id') uno(@Param('id', ParseIntPipe) id: number) { return this.svc.obtener(id); }
  @Roles('Administrador') @Put('usuarios/:id') upd(@Param('id', ParseIntPipe) id: number, @Body() b: any, @Req() r: any) { return this.svc.actualizar(id, b, r.user); }
  @Roles('Administrador') @Patch('usuarios/:id/estado') estado(@Param('id', ParseIntPipe) id: number, @Body() b: any, @Req() r: any) { return this.svc.cambiarEstado(id, b, r.user); }
  @Roles('Administrador') @Post('usuarios/:id/identidades') vincular(@Param('id', ParseIntPipe) id: number, @Body() b: any, @Req() r: any) { return this.svc.vincularIdentidad(id, b, r.user); }
}


/**
 * Login con Google (OpenID Connect) y GitHub (OAuth 2.0) — Authorization Code + PKCE.
 *   GET /api/auth/oauth/providers            proveedores habilitados (para mostrar los botones)
 *   GET /api/auth/oauth/:proveedor/start     redirige al proveedor
 *   GET /api/auth/oauth/:proveedor/callback  recibe el code, valida, crea/vincula el usuario y emite NUESTRO JWT
 * Al terminar, el navegador vuelve al frontend con un refresh token de un solo uso en el fragmento (#rt=...),
 * que la SPA canjea por la sesión en POST /api/auth/refresh (nunca viaja en query ni se registra en servidores).
 */
@Controller('api/auth/oauth')
export class OAuthController {
  private log = new Logger('OAuth');
  constructor(private oauth: OAuthService, private users: UsersService, private audit: AuditClient) {}

  @Public() @Get('providers')
  providers() { return { providers: this.oauth.proveedoresHabilitados() }; }

  @Public() @Get(':proveedor/start')
  async start(@Param('proveedor') prov: string, @Res() res: any) {
    res.setHeader('Cache-Control', 'no-store');
    if (!this.oauth.habilitado(prov)) return res.redirect(302, '/login?oauth_error=proveedor_no_disponible');
    try {
      const { url, cookie } = await this.oauth.iniciar(prov);
      res.setHeader('Set-Cookie', cookie);
      return res.redirect(302, url);
    } catch (e) {
      this.log.error(`No se pudo iniciar el login con ${prov}: ${(e as Error).message}`);
      return res.redirect(302, '/login?oauth_error=proveedor_error');
    }
  }

  @Public() @Get(':proveedor/callback')
  async callback(@Param('proveedor') prov: string, @Query() q: any, @Req() req: any, @Res() res: any) {
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('Set-Cookie', this.oauth.limpiarCookie());   // el contexto del intento es de un solo uso
    if (!this.oauth.habilitado(prov)) return res.redirect(302, '/login?oauth_error=proveedor_no_disponible');
    try {
      const perfil = await this.oauth.completar(prov, q ?? {}, req.headers['cookie']);
      const sesion = await this.users.accederConIdentidadExterna(perfil);
      return res.redirect(302, `/oauth/callback#rt=${encodeURIComponent(sesion.refreshToken)}`);
    } catch (e) {
      const codigo = e instanceof OAuthError ? e.codigo : 'error';
      this.log.warn(`Login con ${prov} rechazado (${codigo}): ${(e as Error).message}`);
      this.audit.registrar(null, 'LOGIN_OAUTH_FALLIDO', `Intento de login con ${prov} rechazado: ${codigo}.`);
      return res.redirect(302, `/login?oauth_error=${encodeURIComponent(codigo)}`);
    }
  }
}
