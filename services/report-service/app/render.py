from datetime import datetime, timezone
from pathlib import Path

from jinja2 import Environment, FileSystemLoader, select_autoescape

from .builders import TZ

_env = Environment(loader=FileSystemLoader(Path(__file__).parent / "templates"), autoescape=select_autoescape(["html", "j2"]))
SYSTEM = "Sistema de Monitoreo y Alerta Temprana de Riesgos Climáticos"


def render_html(report: dict, meta: dict) -> str:
    meta = {**meta, "generatedAtLocal": datetime.now(timezone.utc).astimezone(TZ).strftime("%Y-%m-%d %H:%M")}
    return _env.get_template("report.html.j2").render(r={"level_col": None, **report}, meta=meta, system=SYSTEM)


def html_to_pdf(html: str) -> bytes:
    from weasyprint import HTML  # import diferido: acelera el arranque y las pruebas
    return HTML(string=html).write_pdf()
