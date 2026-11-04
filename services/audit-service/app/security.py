"""Validación del JWT (Issuer, Audience, Firma, Expiración, Roles, Permisos)."""
import os
from dataclasses import dataclass, field

import jwt
from fastapi import Depends, HTTPException
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

_bearer = HTTPBearer(auto_error=False)


def _cfg():
    return (
        os.environ.get("JWT_KEY", ""),
        os.environ.get("JWT_ISSUER", "AlertaClimatica.Auth"),
        os.environ.get("JWT_AUDIENCE", "AlertaClimatica.Client"),
    )


@dataclass
class Principal:
    sub: str
    name: str
    email: str
    roles: list = field(default_factory=list)
    permissions: list = field(default_factory=list)
    token: str = ""


def _as_list(v):
    if v is None:
        return []
    return v if isinstance(v, list) else [v]


def decode_token(token: str) -> Principal:
    key, issuer, audience = _cfg()
    try:
        data = jwt.decode(
            token, key, algorithms=["HS256"], issuer=issuer, audience=audience,
            options={"require": ["exp", "iss", "aud", "sub"]},
        )
    except jwt.PyJWTError as exc:
        raise HTTPException(status_code=401, detail={"mensaje": f"Token inválido: {exc}"})
    return Principal(
        sub=str(data["sub"]), name=data.get("name", ""), email=data.get("email", ""),
        roles=_as_list(data.get("role")), permissions=_as_list(data.get("permissions")),
        token=token,
    )


def current_user(cred: HTTPAuthorizationCredentials = Depends(_bearer)) -> Principal:
    if cred is None:
        raise HTTPException(status_code=401, detail={"mensaje": "Autenticación requerida."})
    return decode_token(cred.credentials)


def require_roles(*roles):
    def dep(user: Principal = Depends(current_user)) -> Principal:
        if not set(roles) & set(user.roles):
            raise HTTPException(status_code=403, detail={"mensaje": "No tiene permisos para esta operación."})
        return user
    return dep
