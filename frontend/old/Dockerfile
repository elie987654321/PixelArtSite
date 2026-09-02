# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies first (cached unless package files change)
COPY package*.json ./
RUN npm ci

# Build the Angular app (outputs static files to dist/pixelart/browser)
COPY . .
RUN npm run build

# ---- Serve stage ----
FROM nginx:alpine AS runtime

# SPA-aware nginx config (falls back to index.html for client-side routes)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy the compiled static files into nginx's web root
COPY --from=build /app/dist/pixelart/browser /usr/share/nginx/html

EXPOSE 80
