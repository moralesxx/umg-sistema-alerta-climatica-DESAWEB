import { Body, Controller, Get, HttpCode, Param, ParseIntPipe, Patch, Post, Put, Query, Req } from '@nestjs/common';
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
  @Post('logout') @HttpCode(200) logout(@Req() r: any) { return this.svc.logout(r.user); }
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
