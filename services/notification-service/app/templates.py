from jinja2 import Environment, select_autoescape

_env = Environment(autoescape=select_autoescape(default=True, default_for_string=True))
COLORS = {"Rojo": "#b00020", "Naranja": "#c96a12", "Amarillo": "#b8860b", "Verde": "#2e7d4f"}

_HTML = _env.from_string("""
<div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;border:1px solid #ddd;border-radius:8px;overflow:hidden">
  <div style="background:{{ color }};color:#fff;padding:16px 20px">
    <div style="font-size:12px;letter-spacing:1px">ALERTA CLIMÁTICA</div>
    <div style="font-size:22px;font-weight:bold">Nivel {{ a.nivelRiesgo }} — {{ a.fenomeno or 'Evento' }}</div>
  </div>
  <div style="padding:18px 20px;color:#222">
    <p style="margin-top:0">{{ a.mensaje }}</p>
    <table style="border-collapse:collapse;width:100%;font-size:14px">
      <tr><td style="padding:6px;color:#666;width:38%">Comunidad</td><td style="padding:6px"><b>{{ a.comunidadNombre or '—' }}</b></td></tr>
      <tr><td style="padding:6px;color:#666">Sensor</td><td style="padding:6px">{{ a.sensorNombre or '—' }}</td></tr>
      <tr><td style="padding:6px;color:#666">Valor detectado</td><td style="padding:6px">{{ valor }}</td></tr>
      {% if a.umbral %}<tr><td style="padding:6px;color:#666">Umbral</td><td style="padding:6px">{{ a.umbral }}</td></tr>{% endif %}
      <tr><td style="padding:6px;color:#666">Fecha</td><td style="padding:6px">{{ fecha }}</td></tr>
      <tr><td style="padding:6px;color:#666">Alerta</td><td style="padding:6px">#{{ a.alertaId }}</td></tr>
    </table>
  </div>
  <div style="background:#f4f6f9;padding:10px 20px;font-size:12px;color:#777">
    Sistema de Monitoreo y Alerta Temprana. Recibe este correo porque está suscrito a alertas de nivel {{ minimo }} o superior.
  </div>
</div>""")


def render_alert(a: dict, minimo: str, fecha: str) -> tuple[str, str, str]:
    nivel = a.get("nivelRiesgo", "")
    valor = a.get("valorDetectado")
    valor_txt = "—" if valor is None else f"{valor} {a.get('unidad') or ''}".strip()
    html = _HTML.render(a=a, color=COLORS.get(nivel, "#555"), valor=valor_txt, fecha=fecha, minimo=minimo)
    subject = f"[ALERTA {nivel.upper()}] {a.get('fenomeno') or 'Evento'} — {a.get('comunidadNombre') or 'Sin comunidad'}"
    text = (f"ALERTA {nivel.upper()} - {a.get('fenomeno') or 'Evento'}\n{a.get('mensaje')}\n"
            f"Comunidad: {a.get('comunidadNombre') or '-'}\nSensor: {a.get('sensorNombre') or '-'}\nValor: {valor_txt}\nFecha: {fecha}\nAlerta #{a.get('alertaId')}")
    return subject, html, text
