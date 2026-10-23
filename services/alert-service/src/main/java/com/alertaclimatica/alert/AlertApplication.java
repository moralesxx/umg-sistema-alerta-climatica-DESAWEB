package com.alertaclimatica.alert;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.domain.EntityScan;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;

@SpringBootApplication
@EntityScan(basePackages = "com.alertaclimatica.alert.domain")
@EnableJpaRepositories(
    basePackages = "com.alertaclimatica.alert.domain",
    considerNestedRepositories = true
)
public class AlertApplication {
    public static void main(String[] args) {
        SpringApplication.run(AlertApplication.class, args);
    }
}