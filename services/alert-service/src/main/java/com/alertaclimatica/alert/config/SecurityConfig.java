package com.alertaclimatica.alert.config;

import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;
import javax.crypto.spec.SecretKeySpec;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.oauth2.core.DelegatingOAuth2TokenValidator;
import org.springframework.security.oauth2.jose.jws.MacAlgorithm;
import org.springframework.security.oauth2.jwt.*;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;
import org.springframework.security.web.SecurityFilterChain;

/** Valida el JWT: firma HS256, Issuer, Audience y expiración. El rol viene en el claim "role". */
@Configuration
@EnableMethodSecurity
public class SecurityConfig {

    @Bean
    SecurityFilterChain chain(HttpSecurity http) throws Exception {
        http.csrf(c -> c.disable())
            .sessionManagement(s -> s.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
            .authorizeHttpRequests(a -> a.requestMatchers("/health", "/actuator/health/**").permitAll().anyRequest().authenticated())
            .oauth2ResourceServer(o -> o.jwt(j -> j.jwtAuthenticationConverter(converter())));
        return http.build();
    }

    @Bean
    JwtDecoder jwtDecoder(@Value("${app.jwt.key}") String key, @Value("${app.jwt.issuer}") String issuer,
                          @Value("${app.jwt.audience}") String audience) {
        if (key == null || key.getBytes(StandardCharsets.UTF_8).length < 32)
            throw new IllegalStateException("JWT_KEY debe tener al menos 32 caracteres.");
        NimbusJwtDecoder d = NimbusJwtDecoder.withSecretKey(new SecretKeySpec(key.getBytes(StandardCharsets.UTF_8), "HmacSHA256"))
            .macAlgorithm(MacAlgorithm.HS256).build();
        d.setJwtValidator(new DelegatingOAuth2TokenValidator<>(
            new JwtTimestampValidator(Duration.ofSeconds(30)),
            new JwtIssuerValidator(issuer),
            new JwtClaimValidator<List<String>>(JwtClaimNames.AUD, aud -> aud != null && aud.contains(audience))));
        return d;
    }

    private JwtAuthenticationConverter converter() {
        JwtAuthenticationConverter c = new JwtAuthenticationConverter();
        c.setJwtGrantedAuthoritiesConverter(jwt -> {
            String role = jwt.getClaimAsString("role");
            return role == null ? List.<GrantedAuthority>of() : List.<GrantedAuthority>of(new SimpleGrantedAuthority("ROLE_" + role));
        });
        return c;
    }
}
