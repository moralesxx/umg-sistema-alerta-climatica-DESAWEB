import { Body, Controller, Get, HttpCode, NotFoundException, Param, ParseIntPipe, Patch, Post, Put, Query, Req } from '@nestjs/common';
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
