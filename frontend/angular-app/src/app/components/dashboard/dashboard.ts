import {
  Component,
  OnInit,
  ChangeDetectorRef,
  OnDestroy
} from '@angular/core';

import { AuthService } from '../../services/auth.service';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  SensorService,
  Sensor
} from '../../services/sensor.service';

import {
  AlertaService,
  Alerta
} from '../../services/alerta.service';

import {
  EventoService,
  Evento
} from '../../services/evento.service';

import {
  BitacoraService
} from '../../services/bitacora.service';


@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit, OnDestroy {

  // =========================================================
  // DATOS METEOROLÓGICOS
  // =========================================================

  temperatura: number = 24.5;
  humedad: number = 68;
  velocidadViento: number = 18.2;
  nivelLluvia: number = 5.4;
  nivelRio: number = 1.8;

  nivelAlertaGlobal: string = 'Verde';


  // =========================================================
  // DATOS DEL DASHBOARD
  // =========================================================

  alertasRecientes: Alerta[] = [];
  sensores: Sensor[] = [];
  eventos: Evento[] = [];


  // =========================================================
  // GRÁFICOS
  // =========================================================

  historialTemperaturas: number[] = [
    22,
    23,
    24,
    24.5
  ];

  historialRios: number[] = [
    1.5,
    1.6,
    1.7,
    1.8
  ];

  svgPathTemperatura: string = '';
  svgPathRio: string = '';


  // =========================================================
  // MODAL AGREGAR SENSOR
  // =========================================================

  showAddSensorModal = false;

  nuevoSensorNombre = '';
  nuevoSensorUbicacion = '';


  // =========================================================
  // MODAL EDITAR SENSOR
  // =========================================================

  showEditSensorModal = false;

  sensorEnEdicionId: number | null = null;

  editSensorNombre = '';
  editSensorUbicacion = '';
  editSensorEstado = true;
  editSensorTipoId = 1;


  // =========================================================
  // MODAL CONFIRMACIÓN
  // =========================================================

  showConfirmModal = false;

  confirmMessage = '';

  private confirmAction: (() => void) | null = null;


  // =========================================================
  // TOAST
  // =========================================================

  showToast = false;
  toastMessage = '';


  // =========================================================
  // INTERVALO DE TIEMPO REAL
  // =========================================================

  private intervaloTiempoReal:
    ReturnType<typeof setInterval> | null = null;


  // =========================================================
  // CONSTRUCTOR
  // =========================================================

  constructor(
    private sensorService: SensorService,
    private alertaService: AlertaService,
    private eventoService: EventoService,
    private bitacoraService: BitacoraService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}


  // =========================================================
  // INICIALIZACIÓN
  // =========================================================

  ngOnInit(): void {

    this.cargarSensores();

    this.cargarAlertas();

    this.cargarEventos();

    this.actualizarGraficos();


    this.intervaloTiempoReal = setInterval(() => {

      this.simularLecturasTiempoReal();

      this.actualizarGraficos();

      this.cdr.detectChanges();

    }, 5000);
  }


  // =========================================================
  // DESTRUIR COMPONENTE
  // =========================================================

  ngOnDestroy(): void {

    if (
      this.intervaloTiempoReal !== null
    ) {

      clearInterval(
        this.intervaloTiempoReal
      );

      this.intervaloTiempoReal = null;
    }
  }


  // =========================================================
  // BITÁCORA
  // =========================================================
  //
  // El UsuarioId NO se envía desde Angular.
  //
  // El backend obtiene el UsuarioId
  // directamente desde el JWT.
  //
  // =========================================================

  private registrarAccion(
    accion: string,
    detalle: string
  ): void {

    this.bitacoraService
      .registrarAccion(
        accion,
        detalle
      )
      .subscribe({

        next: () => {

          console.log(
            `Bitácora registrada: ${accion}`
          );

        },

        error: (err) => {

          console.error(
            `Error al registrar bitácora (${accion}):`,
            err
          );

        }

      });
  }


  // =========================================================
  // CERRAR SESIÓN
  // =========================================================
  //
  // IMPORTANTE:
  //
  // Primero intentamos registrar la acción.
  // Después eliminamos el token.
  //
  // Esto permite que el interceptor pueda enviar
  // el JWT al endpoint de bitácora.
  //
  // =========================================================

  cerrarSesion(): void {

    this.bitacoraService
      .registrarAccion(
        'CERRAR_SESION',
        'El usuario cerró sesión desde el dashboard.'
      )
      .subscribe({

        next: () => {

          console.log(
            'Cierre de sesión registrado en bitácora.'
          );

          this.authService.cerrarSesion();

          window.location.href = '/login';
        },

        error: (err) => {

          console.error(
            'Error al registrar cierre de sesión en bitácora:',
            err
          );

          // Aunque falle la bitácora,
          // permitimos cerrar la sesión.

          this.authService.cerrarSesion();

          window.location.href = '/login';
        }

      });
  }


  // =========================================================
  // CARGAR SENSORES
  // =========================================================

  cargarSensores(): void {

    this.sensorService
      .getSensores()
      .subscribe({

        next: (data) => {

          this.sensores = data;

          this.cdr.detectChanges();

        },

        error: (err) => {

          console.error(
            'Error al cargar sensores:',
            err
          );

        }

      });
  }


  // =========================================================
  // CARGAR ALERTAS
  // =========================================================

  cargarAlertas(): void {

    this.alertaService
      .getAlertas()
      .subscribe({

        next: (data) => {

          this.alertasRecientes = data;

          this.cdr.detectChanges();

        },

        error: (err) => {

          console.error(
            'Error al cargar alertas:',
            err
          );

        }

      });
  }


  // =========================================================
  // CARGAR EVENTOS
  // =========================================================

  cargarEventos(): void {

    this.eventoService
      .getEventos()
      .subscribe({

        next: (data) => {

          this.eventos = data;

          this.cdr.detectChanges();

        },

        error: (err) => {

          console.error(
            'Error al cargar eventos:',
            err
          );

        }

      });
  }


  // =========================================================
  // SIMULAR ALERTA
  // =========================================================

  onSimularAlerta(): void {

    const nivelActual =
      this.nivelAlertaGlobal;

    let mensajePrueba = '';


    switch (
      nivelActual.toLowerCase()
    ) {

      case 'rojo':

        mensajePrueba =
          'Emergencia crítica: Desbordamiento inminente en el cauce principal del río detectado por sensores de caudal. Evacuación requerida.';

        break;


      case 'naranja':

        mensajePrueba =
          'Alerta alta: Condiciones extremas de temperatura y sequedad con riesgo inminente de propagación de incendio forestal.';

        break;


      case 'amarillo':

        mensajePrueba =
          'Precaución: Déficit prolongado de precipitaciones afectando la humedad del suelo y niveles de reservas hídricas (Sequía).';

        break;


      case 'verde':

      default:

        mensajePrueba =
          'Condición normal/estable: Descenso leve de temperatura registrado en zona rural sin riesgo de helada severa.';

        break;
    }


    this.alertaService
      .simularAlerta(
        nivelActual,
        mensajePrueba
      )
      .subscribe({

        next: () => {

          this.registrarAccion(
            'SIMULAR_ALERTA',
            `El usuario simuló una alerta de nivel ${nivelActual}.`
          );


          this.cargarAlertas();

          this.cargarEventos();


          this.mostrarToast(
            'Alerta simulada correctamente.'
          );

        },

        error: (err) => {

          console.error(
            'Error al simular alerta:',
            err
          );

        }

      });
  }


  // =========================================================
  // ABRIR MODAL AGREGAR SENSOR
  // =========================================================

  onAgregarSensor(): void {

    this.nuevoSensorNombre = '';

    this.nuevoSensorUbicacion = '';

    this.showAddSensorModal = true;
  }


  // =========================================================
  // CANCELAR AGREGAR SENSOR
  // =========================================================

  cancelarAgregarSensor(): void {

    this.showAddSensorModal = false;
  }


  // =========================================================
  // CONFIRMAR AGREGAR SENSOR
  // =========================================================

  confirmarAgregarSensor(): void {

    if (
      !this.nuevoSensorNombre.trim() ||
      !this.nuevoSensorUbicacion.trim()
    ) {

      return;
    }


    const nombreSensor =
      this.nuevoSensorNombre.trim();

    const ubicacion =
      this.nuevoSensorUbicacion.trim();


    const nuevoSensor: Sensor = {

      sensorId: 0,

      nombreSensor,

      ubicacion,

      estado: true,

      tipoSensorId: 1
    };


    this.sensorService
      .createSensor(nuevoSensor)
      .subscribe({

        next: () => {

          this.registrarAccion(
            'AGREGAR_SENSOR',
            `El usuario agregó el sensor "${nombreSensor}" en la ubicación "${ubicacion}".`
          );


          this.cargarSensores();


          this.showAddSensorModal = false;


          this.mostrarToast(
            'Sensor agregado correctamente.'
          );

        },

        error: (err) => {

          console.error(
            'Error al crear sensor:',
            err
          );

        }

      });
  }


  // =========================================================
  // ABRIR MODAL EDITAR SENSOR
  // =========================================================

  onEditarSensor(
    sensor: Sensor
  ): void {

    this.sensorEnEdicionId =
      sensor.sensorId;

    this.editSensorNombre =
      sensor.nombreSensor;

    this.editSensorUbicacion =
      sensor.ubicacion;

    this.editSensorEstado =
      sensor.estado;

    this.editSensorTipoId =
      sensor.tipoSensorId || 1;

    this.showEditSensorModal = true;
  }


  // =========================================================
  // CANCELAR EDICIÓN
  // =========================================================

  cancelarEdicionSensor(): void {

    this.showEditSensorModal = false;

    this.sensorEnEdicionId = null;
  }


  // =========================================================
  // CONFIRMAR EDICIÓN
  // =========================================================

  confirmarEdicionSensor(): void {

    if (
      this.sensorEnEdicionId === null ||
      !this.editSensorNombre.trim() ||
      !this.editSensorUbicacion.trim()
    ) {

      return;
    }


    const sensorId =
      this.sensorEnEdicionId;

    const nombreSensor =
      this.editSensorNombre.trim();

    const ubicacion =
      this.editSensorUbicacion.trim();


    const sensorActualizado: Sensor = {

      sensorId,

      nombreSensor,

      ubicacion,

      estado: this.editSensorEstado,

      tipoSensorId: this.editSensorTipoId
    };


    this.sensorService
      .updateSensor(
        sensorId,
        sensorActualizado
      )
      .subscribe({

        next: () => {

          this.registrarAccion(
            'EDITAR_SENSOR',
            `El usuario modificó el sensor #${sensorId}. Nuevo nombre: "${nombreSensor}", ubicación: "${ubicacion}".`
          );


          this.cargarSensores();


          this.showEditSensorModal = false;

          this.sensorEnEdicionId = null;


          this.mostrarToast(
            'Sensor actualizado correctamente.'
          );

        },

        error: (err) => {

          console.error(
            'Error al actualizar sensor:',
            err
          );

        }

      });
  }


  // =========================================================
  // CAMBIAR ESTADO SENSOR
  // =========================================================

  onToggleEstado(
    sensor: Sensor
  ): void {

    const nuevoEstado =
      !sensor.estado;


    this.sensorService
      .cambiarEstadoSensor(
        sensor.sensorId,
        nuevoEstado
      )
      .subscribe({

        next: () => {

          sensor.estado =
            nuevoEstado;


          this.registrarAccion(
            'CAMBIAR_ESTADO_SENSOR',
            `El usuario cambió el estado del sensor #${sensor.sensorId} a ${nuevoEstado ? 'Activo' : 'Inactivo'}.`
          );


          this.cdr.detectChanges();


          this.mostrarToast(
            `Sensor ${nuevoEstado ? 'activado' : 'desactivado'} correctamente.`
          );

        },

        error: (err) => {

          console.error(
            'Error al cambiar estado del sensor:',
            err
          );

        }

      });
  }


  // =========================================================
  // LIMPIAR HISTORIAL
  // =========================================================

  onLimpiarHistorial(): void {

    this.confirmMessage =
      '¿Desea limpiar el historial de alertas?';


    this.confirmAction = () => {

      this.alertaService
        .limpiarHistorial()
        .subscribe({

          next: () => {

            this.alertasRecientes = [];


            this.registrarAccion(
              'LIMPIAR_HISTORIAL_ALERTAS',
              'El usuario limpió el historial de alertas del sistema.'
            );


            this.cdr.detectChanges();


            this.mostrarToast(
              'Historial de alertas limpiado.'
            );

          },

          error: (err) => {

            console.error(
              'Error al limpiar historial:',
              err
            );

          }

        });
    };


    this.showConfirmModal = true;
  }


  // =========================================================
  // REINICIAR SISTEMA
  // =========================================================

  onReiniciarSistema(): void {

    this.confirmMessage =
      '¿Desea reiniciar el sistema general de monitoreo?';


    this.confirmAction = () => {

      this.sensorService
        .reiniciarSistema()
        .subscribe({

          next: () => {

            this.registrarAccion(
              'REINICIAR_SISTEMA',
              'El usuario ejecutó el reinicio general del sistema de monitoreo.'
            );


            this.cargarSensores();

            this.cargarAlertas();

            this.cargarEventos();


            this.mostrarToast(
              'Sistema de monitoreo reiniciado con éxito.'
            );

          },

          error: (err) => {

            console.error(
              'Error al reiniciar sistema:',
              err
            );

          }

        });
    };


    this.showConfirmModal = true;
  }


  // =========================================================
  // EJECUTAR CONFIRMACIÓN
  // =========================================================

  ejecutarConfirmacion(): void {

    if (this.confirmAction) {

      this.confirmAction();
    }


    this.showConfirmModal = false;

    this.confirmAction = null;
  }


  // =========================================================
  // CANCELAR CONFIRMACIÓN
  // =========================================================

  cancelarConfirmacion(): void {

    this.showConfirmModal = false;

    this.confirmAction = null;
  }


  // =========================================================
  // TOAST
  // =========================================================

  mostrarToast(
    mensaje: string
  ): void {

    this.toastMessage =
      mensaje;

    this.showToast = true;


    setTimeout(() => {

      this.showToast = false;

      this.cdr.detectChanges();

    }, 3000);
  }


  // =========================================================
  // SIMULACIÓN DE LECTURAS
  // =========================================================
  //
  // Esta función NO registra bitácora.
  //
  // Se ejecuta automáticamente cada 5 segundos.
  //
  // =========================================================

  simularLecturasTiempoReal(): void {

    this.temperatura =
      Number(
        (
          20 +
          Math.random() * 15
        ).toFixed(1)
      );


    this.humedad =
      Math.floor(
        50 +
        Math.random() * 40
      );


    this.velocidadViento =
      Number(
        (
          10 +
          Math.random() * 30
        ).toFixed(1)
      );


    this.nivelLluvia =
      Number(
        (
          Math.random() * 20
        ).toFixed(1)
      );


    this.nivelRio =
      Number(
        (
          1.2 +
          Math.random() * 2.5
        ).toFixed(2)
      );


    // =======================================================
    // HISTORIAL GRÁFICO
    // =======================================================

    this.historialTemperaturas.push(
      this.temperatura
    );

    this.historialRios.push(
      this.nivelRio
    );


    if (
      this.historialTemperaturas.length > 10
    ) {

      this.historialTemperaturas.shift();
    }


    if (
      this.historialRios.length > 10
    ) {

      this.historialRios.shift();
    }


    // =======================================================
    // NIVEL BASE DE ALERTA
    // =======================================================

    const orden = [
      'Verde',
      'Amarillo',
      'Naranja',
      'Rojo'
    ];


    const rand =
      Math.random();


    let nivelBase: string;


    if (rand < 0.25) {

      nivelBase = 'Verde';

    } else if (rand < 0.50) {

      nivelBase = 'Amarillo';

    } else if (rand < 0.75) {

      nivelBase = 'Naranja';

    } else {

      nivelBase = 'Rojo';
    }


    // =======================================================
    // EVITAR REPETICIÓN DEL NIVEL
    // =======================================================

    if (
      nivelBase ===
      this.nivelAlertaGlobal
    ) {

      const rand2 =
        Math.random();


      if (rand2 < 0.25) {

        nivelBase = 'Verde';

      } else if (rand2 < 0.50) {

        nivelBase = 'Amarillo';

      } else if (rand2 < 0.75) {

        nivelBase = 'Naranja';

      } else {

        nivelBase = 'Rojo';
      }
    }


    // =======================================================
    // NIVEL SEGÚN SENSORES
    // =======================================================

    let nivelSensores =
      'Verde';


    if (
      this.nivelRio > 3.2 ||
      this.nivelLluvia > 15
    ) {

      nivelSensores =
        'Rojo';

    } else if (
      this.nivelRio > 2.7 ||
      this.velocidadViento > 35
    ) {

      nivelSensores =
        'Naranja';

    } else if (
      this.temperatura > 32 ||
      this.humedad < 52
    ) {

      nivelSensores =
        'Amarillo';
    }


    // =======================================================
    // NIVEL FINAL
    // =======================================================

    const indiceFinal =
      Math.max(
        orden.indexOf(nivelBase),
        orden.indexOf(nivelSensores)
      );


    this.nivelAlertaGlobal =
      orden[indiceFinal];
  }


  // =========================================================
  // ACTUALIZAR GRÁFICOS
  // =========================================================

  actualizarGraficos(): void {

    this.svgPathTemperatura =
      this.generarPathSvg(
        this.historialTemperaturas,
        10,
        40
      );


    this.svgPathRio =
      this.generarPathSvg(
        this.historialRios,
        0,
        5
      );
  }


  // =========================================================
  // GENERAR PATH SVG
  // =========================================================

  private generarPathSvg(
    datos: number[],
    minVal: number,
    maxVal: number
  ): string {

    if (
      !datos ||
      datos.length === 0
    ) {

      return '';
    }


    const width =
      500;

    const height =
      150;


    const step =
      width /
      Math.max(
        datos.length - 1,
        1
      );


    return datos
      .map((val, i) => {

        const x =
          i * step;


        const normalizedVal =
          Math.min(
            Math.max(
              val,
              minVal
            ),
            maxVal
          );


        const y =
          height -
          (
            (
              normalizedVal -
              minVal
            ) /
            (
              maxVal -
              minVal
            )
          ) *
          height;


        return `${
          i === 0
            ? 'M'
            : 'L'
        } ${
          x.toFixed(1)
        } ${
          y.toFixed(1)
        }`;

      })
      .join(' ');
  }


  // =========================================================
  // CLASE DEL BADGE
  // =========================================================

  getBadgeClass(
    texto: any
  ): string {

    const n =
      String(
        texto || ''
      ).toLowerCase();


    if (
      n.includes('rojo') ||
      n.includes('critica') ||
      n.includes('crítica')
    ) {

      return 'bg-danger text-white';
    }


    if (
      n.includes('naranja')
    ) {

      return 'bg-warning-orange text-white';
    }


    if (
      n.includes('amarillo')
    ) {

      return 'bg-warning text-dark fw-bold';
    }


    if (
      n.includes('verde')
    ) {

      return 'bg-success text-white';
    }


    return 'bg-secondary text-white';
  }
}