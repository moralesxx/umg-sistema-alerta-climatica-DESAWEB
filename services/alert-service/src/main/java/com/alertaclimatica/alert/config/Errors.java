package com.alertaclimatica.alert.config;

import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;

@RestControllerAdvice
public class Errors {
    private static final Logger log = LoggerFactory.getLogger(Errors.class);

    public static class Conflict extends RuntimeException { public Conflict(String m) { super(m); } }
    public static class NotFound extends RuntimeException { public NotFound(String m) { super(m); } }

    private static ResponseEntity<Map<String, String>> r(HttpStatus s, String m) { return ResponseEntity.status(s).body(Map.of("mensaje", m)); }

    @ExceptionHandler(IllegalArgumentException.class) ResponseEntity<?> bad(IllegalArgumentException e) { return r(HttpStatus.BAD_REQUEST, e.getMessage()); }
    @ExceptionHandler({HttpMessageNotReadableException.class, MethodArgumentTypeMismatchException.class}) ResponseEntity<?> body(Exception e) { return r(HttpStatus.BAD_REQUEST, "Solicitud inválida."); }
    @ExceptionHandler(Conflict.class) ResponseEntity<?> conflict(Conflict e) { return r(HttpStatus.CONFLICT, e.getMessage()); }
    @ExceptionHandler(NotFound.class) ResponseEntity<?> nf(NotFound e) { return r(HttpStatus.NOT_FOUND, e.getMessage()); }
    @ExceptionHandler(AccessDeniedException.class) ResponseEntity<?> denied(AccessDeniedException e) { return r(HttpStatus.FORBIDDEN, "No tiene permisos para esta operación."); }
    @ExceptionHandler(Exception.class) ResponseEntity<?> other(Exception e) { log.error("Error no controlado", e); return r(HttpStatus.INTERNAL_SERVER_ERROR, "Error interno del servicio."); }
}
