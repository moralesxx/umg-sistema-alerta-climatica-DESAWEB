package com.alertaclimatica.alert.service;

import java.math.BigDecimal;
import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;
import java.time.*;
import java.time.format.DateTimeParseException;
import java.util.List;
import java.util.Locale;

public final class Util {
    private Util() {}
    public static final List<String> NIVELES = List.of("Verde", "Amarillo", "Naranja", "Rojo");
    public static final List<String> FENOMENOS = List.of("Inundación", "Sequía", "Tormenta", "Helada", "Incendio forestal");
    public static final List<String> ESTADOS = List.of("Activa", "Atendida", "Cerrada");
    public static final ZoneId TZ = ZoneId.of(System.getenv().getOrDefault("APP_TZ", "America/Guatemala"));

    public static int peso(String nivel) {
        return switch (nivel == null ? "" : nivel.trim().toLowerCase(Locale.ROOT)) { case "amarillo" -> 1; case "naranja" -> 2; case "rojo" -> 3; default -> 0; };
    }

    public static String canon(List<String> valores, String v) {
        if (v == null) return null;
        return valores.stream().filter(x -> x.equalsIgnoreCase(v.trim())).findFirst().orElse(null);
    }

    public static String recortar(String s, int max) { return s.length() <= max ? s : s.substring(0, max); }

    public static String num(BigDecimal v) {
        return new DecimalFormat("0.##", DecimalFormatSymbols.getInstance(Locale.ROOT)).format(v);
    }

    /** "yyyy-MM-dd" = día en la zona de la app; si no, instante ISO (con offset/Z) o fecha-hora local. Límite superior EXCLUSIVO. */
    public static Instant parse(String s, boolean upper) {
        if (s == null || s.isBlank()) return null;
        s = s.trim();
        try {
            if (s.length() == 10) {
                Instant start = LocalDate.parse(s).atStartOfDay(TZ).toInstant();
                return upper ? LocalDate.parse(s).plusDays(1).atStartOfDay(TZ).toInstant() : start;
            }
            Instant i;
            try { i = OffsetDateTime.parse(s).toInstant(); }
            catch (DateTimeParseException e) { i = LocalDateTime.parse(s).atZone(TZ).toInstant(); }
            return upper ? i.plusMillis(1) : i;
        } catch (DateTimeParseException e) { throw new IllegalArgumentException("Fecha no válida: " + s); }
    }
}
