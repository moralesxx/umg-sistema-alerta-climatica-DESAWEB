import 'reflect-metadata';
import { Logger } from '@nestjs/common';
import { NestFactory } from '@nestjs/core';
import { json } from 'express';
import { DataSource } from 'typeorm';
import { AppModule } from './app.module';
import { validarConfig } from './config';
import { seed } from './seed';

async function bootstrap() {
  validarConfig();
  const app = await NestFactory.create(AppModule, { bodyParser: false });
  app.use(json({ strict: false })); // el frontend envía `true`/`false` crudo en PATCH .../estado
  app.enableShutdownHooks(); // graceful shutdown (SIGTERM de Docker/Kubernetes)
  await seed(app.get(DataSource));
  const port = Number(process.env.PORT ?? 8080);
  await app.listen(port, '0.0.0.0');
  new Logger('Auth').log(`Auth Service escuchando en :${port}`);
}
bootstrap().catch((e) => { console.error(e); process.exit(1); });
