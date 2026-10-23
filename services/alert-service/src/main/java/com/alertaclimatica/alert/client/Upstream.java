package com.alertaclimatica.alert.client;

import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.JWSHeader;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jwt.JWTClaimsSet;
import com.nimbusds.jwt.SignedJWT;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.client.SimpleClientHttpRequestFactory;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

/** Llamadas a otros microservicios SOLO por API. Incluye token de servicio, directorios en caché y cliente de auditoría. */
@Component
public class Upstream {
    private static final Logger log = LoggerFactory.getLogger(Upstream.class);
    private final RestClient http;
    private final String key, issuer, audience, community, sensor, audit;
    private String cached; private long cachedExp;

    public record Tipo(Integer tipoSensorId, String nombreTipo, String unidadMedida) {}
    public record Com(Integer comunidadId, String nombre) {}
    public record SensorInfo(Integer sensorId, String nombreSensor, String codigo, Integer comunidadId, Integer tipoSensorId, Tipo tipoSensor) {}

    public Upstream(@Value("${app.jwt.key}") String key, @Value("${app.jwt.issuer}") String issuer, @Value("${app.jwt.audience}") String audience,
                    @Value("${app.services.community}") String community, @Value("${app.services.sensor}") String sensor,
                    @Value("${app.services.audit}") String audit) {
        this.key = key; this.issuer = issuer; this.audience = audience;
        this.community = community.replaceAll("/$", ""); this.sensor = sensor.replaceAll("/$", ""); this.audit = audit.replaceAll("/$", "");
        SimpleClientHttpRequestFactory f = new SimpleClientHttpRequestFactory();
        f.setConnectTimeout(Duration.ofSeconds(3)); f.setReadTimeout(Duration.ofSeconds(8));
        this.http = RestClient.builder().requestFactory(f).build();
    }

    public synchronized String serviceToken() {
        long now = System.currentTimeMillis();
        if (cached != null && now < cachedExp - 30_000) return cached;
        try {
            cachedExp = now + 300_000;
            JWTClaimsSet c = new JWTClaimsSet.Builder().subject("svc:alert-service").issuer(issuer).audience(audience)
                .claim("role", "Servicio").issueTime(new Date(now)).expirationTime(new Date(cachedExp)).build();
            SignedJWT jwt = new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), c);
            jwt.sign(new MACSigner(key.getBytes(StandardCharsets.UTF_8)));
            return cached = jwt.serialize();
        } catch (Exception e) { throw new IllegalStateException("No se pudo firmar el token de servicio", e); }
    }

    /** Token del usuario actual (para reenviar) o, si no hay usuario, token de servicio. */
    private String bearer(boolean asService) {
        if (!asService) {
            Authentication a = SecurityContextHolder.getContext().getAuthentication();
            if (a != null && a.getPrincipal() instanceof Jwt j) return j.getTokenValue();
        }
        return serviceToken();
    }

    public <T> T get(String base, String path, ParameterizedTypeReference<T> type, boolean asService) {
        return http.get().uri(base + path).header("Authorization", "Bearer " + bearer(asService)).retrieve().body(type);
    }

    // ------------------------------------------------------------ directorios (caché 60 s, degradación elegante)
    private volatile Map<Integer, String> comNames = Map.of(); private volatile long comAt;
    private volatile Map<Integer, Tipo> tipos = Map.of(); private volatile long tiposAt;

    public String comunidadNombre(Integer id) {
        if (id == null) return null;
        if (System.currentTimeMillis() - comAt > 60_000 || !comNames.containsKey(id)) {
            try {
                List<Com> l = get(community, "/api/communities", new ParameterizedTypeReference<List<Com>>() {}, true);
                Map<Integer, String> m = new HashMap<>(); for (Com c : l) m.put(c.comunidadId(), c.nombre());
                comNames = m; comAt = System.currentTimeMillis();
            } catch (Exception e) { log.warn("Community Service no disponible: {}", e.getMessage()); comAt = System.currentTimeMillis() - 50_000; }
        }
        return comNames.get(id);
    }

    public Map<Integer, Tipo> tipos() {
        if (System.currentTimeMillis() - tiposAt > 60_000 || tipos.isEmpty()) {
            try {
                List<Tipo> l = get(sensor, "/api/sensors/types", new ParameterizedTypeReference<List<Tipo>>() {}, true);
                Map<Integer, Tipo> m = new HashMap<>(); for (Tipo t : l) m.put(t.tipoSensorId(), t);
                tipos = m; tiposAt = System.currentTimeMillis();
            } catch (Exception e) { log.warn("Sensor Service no disponible: {}", e.getMessage()); tiposAt = System.currentTimeMillis() - 50_000; }
        }
        return tipos;
    }

    /** Datos del sensor consultados por API con el token del usuario; null si no existe. */
    public SensorInfo sensor(int id) {
        try { return get(sensor, "/api/sensors/" + id, new ParameterizedTypeReference<SensorInfo>() {}, false); }
        catch (org.springframework.web.client.HttpClientErrorException.NotFound e) { return null; }
        catch (org.springframework.web.client.HttpClientErrorException e) { throw e; }
    }

    // ------------------------------------------------------------ auditoría (mejor esfuerzo, asíncrona)
    public void audit(String accion, String detalle, String entidad, Integer entidadId) {
        Integer uid = null; String nombre = null;
        Authentication a = SecurityContextHolder.getContext().getAuthentication();
        if (a != null && a.getPrincipal() instanceof Jwt j) {
            try { uid = Integer.valueOf(j.getSubject()); } catch (Exception ignored) { }
            nombre = j.getClaimAsString("name");
        }
        Map<String, Object> body = new HashMap<>();
        body.put("usuarioId", uid); body.put("usuarioNombre", nombre); body.put("accion", accion); body.put("detalle", detalle);
        body.put("entidad", entidad); body.put("entidadId", entidadId); body.put("servicio", "alert-service");
        String token = serviceToken();
        Thread.startVirtualThread(() -> {
            try { http.post().uri(audit + "/api/audit/internal").header("Authorization", "Bearer " + token).body(body).retrieve().toBodilessEntity(); }
            catch (Exception e) { log.warn("No se pudo registrar auditoría: {}", e.getMessage()); }
        });
    }
}
