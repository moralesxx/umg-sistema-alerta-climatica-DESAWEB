import 'reflect-metadata';
import { ArgumentsHost, Catch, ExceptionFilter, HttpStatus, Logger } from '@nestjs/common';
import { NestFactory } from '@nestjs/core';
import { json } from 'express';
import { DataSource, QueryFailedError } from 'typeorm';
import { AppModule } from './app.module';
import { validarConfig } from './config';
import { seed } from './seed';

/** Violación de unicidad (p. ej. correo duplicado en una carrera) -> 409 en lugar de 500. */
@Catch(QueryFailedError)
class UniqueViolationFilter implements ExceptionFilter {
  catch(e: QueryFailedError & { code?: string }, host: ArgumentsHost) {
    const res = host.switchToHttp().getResponse();
    if ((e as any).code === '23505') return res.status(HttpStatus.CONFLICT).json({ mensaje: 'Ya existe un registro con esos datos.' });
    new Logger('DB').error(e.message);
    return res.status(HttpStatus.INTERNAL_SERVER_ERROR).json({ mensaje: 'Error interno del servicio.' });
  }
}

async function bootstrap() {
  validarConfig();
  const app = await NestFactory.create(AppModule, { bodyParser: false });
  app.use(json({ strict: false })); // el frontend envía `true`/`false` crudo en PATCH .../estado
  app.useGlobalFilters(new UniqueViolationFilter());
  app.enableShutdownHooks(); // graceful shutdown (SIGTERM de Docker/Kubernetes)
  await seed(app.get(DataSource));
  const port = Number(process.env.PORT ?? 8080);
  await app.listen(port, '0.0.0.0');
  new Logger('Auth').log(`Auth Service escuchando en :${port}`);
}
bootstrap().catch((e) => { console.error(e); process.exit(1); });
