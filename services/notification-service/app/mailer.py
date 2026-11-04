"""Envío de correo por SMTP (biblioteca estándar). Configurable por variables de entorno:
SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM, SMTP_SECURITY (none | starttls | ssl).
Gmail: smtp.gmail.com, 587, starttls, y una "contraseña de aplicación" (no la contraseña normal)."""
import os
import re
import smtplib
import ssl
from email.message import EmailMessage
from email.utils import formataddr, parseaddr


def smtp_config() -> dict:
    return {
        "host": os.environ.get("SMTP_HOST", "mailpit"),
        "port": int(os.environ.get("SMTP_PORT", "1025")),
        "user": os.environ.get("SMTP_USER", ""),
        "password": os.environ.get("SMTP_PASSWORD", ""),
        "from": os.environ.get("SMTP_FROM", "Alerta Climática <alertas@sistema-climatico.example>"),
        "security": os.environ.get("SMTP_SECURITY", "none").lower(),
    }


def clean_header(v: str) -> str:
    """Evita inyección de cabeceras: sin saltos de línea y longitud acotada."""
    return re.sub(r"[\r\n]+", " ", v).strip()[:200]


def send_email(to: str, subject: str, html: str, text: str) -> tuple[bool, str]:
    cfg = smtp_config()
    msg = EmailMessage()
    name, addr = parseaddr(cfg["from"])
    msg["From"] = formataddr((clean_header(name), clean_header(addr)))
    msg["To"] = clean_header(to)
    msg["Subject"] = clean_header(subject)
    msg.set_content(text)
    msg.add_alternative(html, subtype="html")
    try:
        if cfg["security"] == "ssl":
            server = smtplib.SMTP_SSL(cfg["host"], cfg["port"], timeout=15, context=ssl.create_default_context())
        else:
            server = smtplib.SMTP(cfg["host"], cfg["port"], timeout=15)
        with server:
            if cfg["security"] == "starttls":
                server.starttls(context=ssl.create_default_context())
            if cfg["user"]:
                server.login(cfg["user"], cfg["password"])
            server.send_message(msg)
        return True, ""
    except Exception as exc:  # noqa: BLE001 — se registra el motivo en la bitácora de notificaciones
        return False, f"{type(exc).__name__}: {str(exc)[:200]}"
