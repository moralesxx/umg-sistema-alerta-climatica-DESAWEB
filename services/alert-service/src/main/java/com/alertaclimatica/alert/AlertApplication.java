package com.alertaclimatica.alert;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;

@SpringBootApplication
// Los repositorios están declarados como interfaces anidadas dentro de Repos; Spring Data las ignora
// por defecto ("Found 0 JPA repository interfaces"), por eso se habilita explícitamente.
@EnableJpaRepositories(considerNestedRepositories = true)
public class AlertApplication {
    public static void main(String[] args) {
        SpringApplication.run(AlertApplication.class, args);
    }
}