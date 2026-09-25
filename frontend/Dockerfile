FROM node:22-alpine AS build
WORKDIR /app

# Copia los archivos de dependencias desde la subcarpeta angular-app
COPY frontend/angular-app/package*.json ./
RUN npm install

# Copia el código fuente de Angular
COPY frontend/angular-app/ ./
RUN npm run build -- --configuration production

FROM nginx:alpine
# Angular genera los archivos estáticos en la carpeta browser dentro de dist
COPY --from=build /app/dist/*/browser /usr/share/nginx/html

# Copia la configuración personalizada de Nginx para soportar rutas de Angular (SPA)
COPY frontend/angular-app/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]