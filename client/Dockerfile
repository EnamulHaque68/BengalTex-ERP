# syntax=docker/dockerfile:1
# Build context = ./client
#   docker build -t bengaltex-web ./client

FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# Production build → dist/client/browser (Angular @angular/build:application output).
RUN npx ng build --configuration production

FROM nginx:alpine AS runtime
# SPA static assets
COPY --from=build /app/dist/client/browser /usr/share/nginx/html
# SPA routing + reverse proxy to the API (see nginx.conf)
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
