import { Module } from '@nestjs/common';
import { APP_GUARD } from '@nestjs/core';
import { JwtModule } from '@nestjs/jwt';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AuditClient } from './audit.client';
import { AuthController, HealthController } from './auth.controller';
import { IdentidadExterna, Rol, Usuario } from './entities';
import { JwtAuthGuard } from './security';
import { UsersService } from './users.service';

@Module({
  imports: [
    TypeOrmModule.forRoot({
      type: 'postgres',
      url: process.env.DATABASE_URL,
      entities: [Rol, Usuario, IdentidadExterna],
      // En producción real con Kubernetes se recomiendan migraciones; aquí el esquema se crea al iniciar.
      synchronize: (process.env.DB_SYNCHRONIZE ?? 'true') === 'true',
      retryAttempts: 20, retryDelay: 3000,
    }),
    TypeOrmModule.forFeature([Rol, Usuario, IdentidadExterna]),
    JwtModule.register({}),
  ],
  controllers: [AuthController, HealthController],
  providers: [UsersService, AuditClient, { provide: APP_GUARD, useClass: JwtAuthGuard }],
})
export class AppModule {}
