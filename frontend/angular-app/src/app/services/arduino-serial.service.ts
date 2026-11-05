
import { Injectable } from '@angular/core';
import { BehaviorSubject, firstValueFrom, Subscription, interval } from 'rxjs';

import {
  SensorService,
  LecturaArduino
} from './sensor.service';

import { AlertaService } from './alerta.service';

// ==========================================================
// TIPOS PARA WEB SERIAL
// ==========================================================

interface PuertoSerialArduino {
  readable: ReadableStream<Uint8Array> | null;
  writable: WritableStream<Uint8Array> | null;
  open(options: { baudRate: number }): Promise<void>;
  close(): Promise<void>;
}

interface NavegadorSerial {
  requestPort(): Promise<PuertoSerialArduino>;
}

declare global {
  interface Navigator {
    serial?: NavegadorSerial;
  }
}

// ==========================================================
// ESTADO DE LA CONEXIÓN
// ==========================================================

export interface EstadoArduino {
  arduinoConectado: boolean;
  conectandoArduino: boolean;
  desconectandoArduino: boolean;
  enviandoLecturas: boolean;
  estadoConexionArduino: string;
  mensajeArduino: string;
  lecturasAceptadas: number;
  lecturasRechazadas: number;
  ultimaLecturaArduino: string;
}

const ESTADO_INICIAL: EstadoArduino = {
  arduinoConectado: false,
  conectandoArduino: false,
  desconectandoArduino: false,
  enviandoLecturas: false,
  estadoConexionArduino: 'Desconectado',
  mensajeArduino: 'Conecta tu Arduino UNO mediante USB para comenzar.',
  lecturasAceptadas: 0,
  lecturasRechazadas: 0,
  ultimaLecturaArduino: ''
};

// ==========================================================
// SERVICIO GLOBAL
// ==========================================================

@Injectable({
  providedIn: 'root'
})
export class ArduinoSerialService {

  private estadoSubject =
    new BehaviorSubject<EstadoArduino>({ ...ESTADO_INICIAL });

  readonly estado$ = this.estadoSubject.asObservable();

  get estado(): EstadoArduino {
    return this.estadoSubject.value;
  }

  private puertoSerial: PuertoSerialArduino | null = null;

  private lectorSerial:
    ReadableStreamDefaultReader<Uint8Array> | null = null;

  private escritorSerial:
    WritableStreamDefaultWriter<Uint8Array> | null = null;

  private tareaLectura: Promise<void> | null = null;

  private detenerLectura = false;

  private bufferSerial = '';

  private colaEnvios: Promise<void> = Promise.resolve();

  private colaComandos: Promise<void> = Promise.resolve();

  private refrescoAlertaSub?: Subscription;

  private ultimoNivelEnviado = '';

  // Envíos de lecturas en espera: si el servidor se atrasa, se descartan los más viejos
  // para que el dashboard muestre siempre lo más reciente en lugar de acumular una cola.
  private enviosPendientes = 0;
  private readonly MAX_ENVIOS_PENDIENTES = 3;

  private nivelAlertaActual = 'Verde';

  constructor(
    private sensorService: SensorService,
    private alertaService: AlertaService
  ) {}

  private actualizarEstado(cambios: Partial<EstadoArduino>): void {
    this.estadoSubject.next({
      ...this.estadoSubject.value,
      ...cambios
    });
  }

  // ==========================================================
  // CONECTAR ARDUINO
  // ==========================================================

  async conectar(): Promise<void> {
    if (!navigator.serial) {
      this.actualizarEstado({
        estadoConexionArduino: 'Error',
        mensajeArduino: 'El navegador no permite utilizar Web Serial.'
      });
      return;
    }

    if (
      this.estado.arduinoConectado ||
      this.estado.conectandoArduino
    ) {
      return;
    }

    try {
      this.actualizarEstado({
        conectandoArduino: true,
        estadoConexionArduino: 'Conectando',
        mensajeArduino: 'Selecciona el puerto USB de tu Arduino UNO.'
      });

      const puerto = await navigator.serial.requestPort();

      this.puertoSerial = puerto;

      await puerto.open({ baudRate: 9600 });

      if (!puerto.readable || !puerto.writable) {
        throw new Error('El puerto serial no está disponible para lectura y escritura.');
      }

      const lector = puerto.readable.getReader();

      this.lectorSerial = lector;
      this.escritorSerial = puerto.writable.getWriter();

      this.detenerLectura = false;
      this.bufferSerial = '';
      this.ultimoNivelEnviado = '';

      this.actualizarEstado({
        arduinoConectado: true,
        conectandoArduino: false,
        estadoConexionArduino: 'Conectado',
        mensajeArduino: 'Arduino conectado correctamente. Esperando lecturas...'
      });

      this.iniciarActualizacionAlerta();

      this.tareaLectura = this.leerDatosArduino(lector);

    } catch (error: any) {
      console.error('Error al conectar Arduino:', error);

      const cancelado = error?.name === 'NotFoundError';

      this.actualizarEstado({
        arduinoConectado: false,
        conectandoArduino: false,
        estadoConexionArduino: cancelado ? 'Desconectado' : 'Error',
        mensajeArduino: cancelado
          ? 'No se seleccionó ningún puerto USB.'
          : error?.message || 'No fue posible conectar el Arduino.'
      });

      this.detenerActualizacionAlerta();
      await this.liberarPuertoSinLector();
    }
  }

  // ==========================================================
  // ACTUALIZACIÓN DEL NIVEL GLOBAL DE ALERTA
  // ==========================================================

  private iniciarActualizacionAlerta(): void {
    this.detenerActualizacionAlerta();

    // Consulta inmediata al conectar.
    this.consultarNivelAlerta();

    // Después consulta cada 5 s y reenvía el nivel (latido): el Arduino vuelve a Verde
    // si pasan 30 s sin recibir comandos, así que no basta con enviarlo solo al cambiar.
    this.refrescoAlertaSub = interval(5000).subscribe(() => {
      this.consultarNivelAlerta();
    });
  }

  private detenerActualizacionAlerta(): void {
    this.refrescoAlertaSub?.unsubscribe();
    this.refrescoAlertaSub = undefined;
  }

  private consultarNivelAlerta(): void {
    if (!this.estado.arduinoConectado || this.detenerLectura) {
      return;
    }

    // El nivel global lo mantiene actualizado el dashboard / la alarma global (AlertaService);
    // aquí solo se reenvía al Arduino como latido para que el buzzer siga sonando.
    this.enviarNivelAlerta(this.alertaService.nivelActual);
  }

  private enviarNivelAlerta(nivel: string): void {
    const nivelesValidos = ['Verde', 'Amarillo', 'Naranja', 'Rojo'];

    const nivelCanonico = nivelesValidos.find(
      n => n.toLowerCase() === nivel.toLowerCase()
    );

    if (!nivelCanonico || !this.estado.arduinoConectado) {
      return;
    }

    this.nivelAlertaActual = nivelCanonico;

    this.ultimoNivelEnviado = nivelCanonico;

    const comando = `NIVEL:${nivelCanonico.toUpperCase()}\n`;

    this.colaComandos = this.colaComandos.then(async () => {
      if (!this.escritorSerial || !this.estado.arduinoConectado) {
        return;
      }

      try {
        const datos = new TextEncoder().encode(comando);
        await this.escritorSerial.write(datos);


      } catch (error) {
        console.error('Error enviando nivel a Arduino:', error);

        this.ultimoNivelEnviado = '';
      }
    });
  }

  // ==========================================================
  // LEER DATOS DEL PUERTO SERIAL
  // ==========================================================

  private async leerDatosArduino(
    lector: ReadableStreamDefaultReader<Uint8Array>
  ): Promise<void> {

    const decoder = new TextDecoder();

    try {
      while (!this.detenerLectura) {
        const { value, done } = await lector.read();

        if (done) {
          break;
        }

        if (value) {
          this.bufferSerial += decoder.decode(value, { stream: true });

          let posicionSalto = this.bufferSerial.indexOf('\n');

          while (posicionSalto !== -1) {
            const linea = this.bufferSerial
              .substring(0, posicionSalto)
              .trim();

            this.bufferSerial =
              this.bufferSerial.substring(posicionSalto + 1);

            if (linea.length > 0) {
              this.procesarLineaArduino(linea);
            }

            posicionSalto = this.bufferSerial.indexOf('\n');
          }
        }
      }

    } catch (error) {
      if (!this.detenerLectura) {
        console.error('Error leyendo el puerto serial:', error);

        this.actualizarEstado({
          estadoConexionArduino: 'Error',
          mensajeArduino: 'Se perdió la comunicación con Arduino.'
        });
      }

    } finally {
      this.detenerActualizacionAlerta();

      try {
        lector.releaseLock();
      } catch (error) {
        console.warn('No se pudo liberar el lector serial:', error);
      }

      if (this.lectorSerial === lector) {
        this.lectorSerial = null;
      }

      const escritor = this.escritorSerial;
      this.escritorSerial = null;

      if (escritor) {
        try {
          await escritor.close();
        } catch (error) {
          console.warn('No se pudo cerrar el escritor serial:', error);
        }
      }

      const puerto = this.puertoSerial;
      this.puertoSerial = null;

      if (puerto) {
        try {
          await puerto.close();
        } catch (error) {
          console.warn('No se pudo cerrar el puerto serial:', error);
        }
      }

      const errorActivo =
        this.estado.estadoConexionArduino === 'Error';

      this.actualizarEstado({
        arduinoConectado: false,
        conectandoArduino: false,
        desconectandoArduino: false,
        estadoConexionArduino: this.detenerLectura
          ? 'Desconectado'
          : errorActivo ? 'Error' : 'Desconectado',
        mensajeArduino: this.detenerLectura
          ? 'Arduino desconectado.'
          : errorActivo
            ? this.estado.mensajeArduino
            : 'La comunicación serial ha finalizado.'
      });

      this.tareaLectura = null;
    }
  }

  // ==========================================================
  // INTERPRETAR JSON RECIBIDO
  // ==========================================================

  private procesarLineaArduino(linea: string): void {
    try {
      const datos = JSON.parse(linea);

      if (!datos || !Array.isArray(datos.lecturas)) {
        console.warn('Formato JSON no reconocido:', datos);
        return;
      }

      const lecturas: LecturaArduino[] = datos.lecturas
        .filter((item: any) =>
          item &&
          typeof item.codigo === 'string' &&
          item.codigo.trim().length > 0 &&
          typeof item.valor === 'number' &&
          Number.isFinite(item.valor)
        )
        .map((item: any) => ({
          codigo: item.codigo.trim(),
          valor: item.valor
        }));

      if (lecturas.length === 0) {
        console.warn('El mensaje no contiene lecturas válidas.');
        return;
      }

      this.actualizarEstado({
        ultimaLecturaArduino: new Date().toLocaleTimeString()
      });

      if (this.enviosPendientes >= this.MAX_ENVIOS_PENDIENTES) {
        console.warn('Servidor lento: se descarta una lectura antigua.');
        return;
      }

      this.enviosPendientes++;

      this.colaEnvios = this.colaEnvios.then(async () => {
        this.actualizarEstado({
          enviandoLecturas: true,
          mensajeArduino: 'Enviando lecturas al servidor...'
        });

        try {
          const respuesta = await firstValueFrom(
            this.sensorService.ingestBrowser(lecturas)
          );

          this.actualizarEstado({
            lecturasAceptadas:
              this.estado.lecturasAceptadas + (respuesta.aceptadas || 0),

            lecturasRechazadas:
              this.estado.lecturasRechazadas +
              (respuesta.rechazadas?.length || 0),

            mensajeArduino:
              `${respuesta.aceptadas} lectura(s) aceptada(s). ` +
              `${respuesta.rechazadas?.length || 0} rechazada(s).`
          });

        } catch (error: any) {
          console.error('Error enviando lecturas al servidor:', error);

          const mensaje =
            error?.error?.mensaje ||
            error?.message ||
            'No fue posible enviar las lecturas.';

          this.actualizarEstado({
            mensajeArduino: mensaje
          });

        } finally {
          this.enviosPendientes = Math.max(0, this.enviosPendientes - 1);

          this.actualizarEstado({
            enviandoLecturas: false
          });
        }
      });

    } catch (error) {
      console.error('No se pudo interpretar el JSON de Arduino:', error);

      this.actualizarEstado({
        mensajeArduino: 'Se recibió un mensaje JSON inválido.'
      });
    }
  }

  // ==========================================================
  // DESCONECTAR ARDUINO
  // ==========================================================

  async desconectar(): Promise<void> {
    if (this.estado.desconectandoArduino) {
      return;
    }

    const lector = this.lectorSerial;
    const tarea = this.tareaLectura;

    if (!lector && !this.puertoSerial) {
      this.actualizarEstado({
        arduinoConectado: false,
        conectandoArduino: false,
        estadoConexionArduino: 'Desconectado'
      });
      return;
    }

    this.actualizarEstado({
      desconectandoArduino: true,
      estadoConexionArduino: 'Desconectando',
      mensajeArduino: 'Cerrando comunicación USB...'
    });

    this.detenerLectura = true;
    this.detenerActualizacionAlerta();

    // Solicita apagar el buzzer antes de cerrar el puerto.
    this.ultimoNivelEnviado = '';
    this.enviarComandoApagado();

    if (lector) {
      try {
        await lector.cancel();
      } catch (error) {
        console.warn('Error al cancelar la lectura serial:', error);
      }
    } else {
      await this.liberarPuertoSinLector();
    }

    if (tarea) {
      try {
        await tarea;
      } catch (error) {
        console.warn('Error finalizando la lectura serial:', error);
      }
    }

    this.actualizarEstado({
      arduinoConectado: false,
      conectandoArduino: false,
      desconectandoArduino: false,
      estadoConexionArduino: 'Desconectado',
      mensajeArduino: 'Arduino desconectado.'
    });
  }

  private enviarComandoApagado(): void {
    if (!this.escritorSerial) {
      return;
    }

    this.colaComandos = this.colaComandos.then(async () => {
      if (!this.escritorSerial) {
        return;
      }

      try {
        await this.escritorSerial.write(
          new TextEncoder().encode('NIVEL:VERDE\n')
        );
      } catch (error) {
        console.warn('No se pudo enviar el apagado del buzzer:', error);
      }
    });
  }

  private async liberarPuertoSinLector(): Promise<void> {
    if (this.lectorSerial) {
      return;
    }

    const escritor = this.escritorSerial;
    this.escritorSerial = null;

    if (escritor) {
      try {
        await escritor.close();
      } catch (error) {
        console.warn('Error cerrando el escritor USB:', error);
      }
    }

    const puerto = this.puertoSerial;
    this.puertoSerial = null;

    if (puerto) {
      try {
        await puerto.close();
      } catch (error) {
        console.warn('Error cerrando el puerto USB:', error);
      }
    }
  }
}
