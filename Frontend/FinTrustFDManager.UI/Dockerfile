# ═══════════════════════════════════════════════════════════════
#  Stage 1: Build Angular app
# ═══════════════════════════════════════════════════════════════
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files first for layer caching
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# Copy source and build
COPY . .
RUN npm run build -- --configuration=production

# ═══════════════════════════════════════════════════════════════
#  Stage 2: Serve with nginx
# ═══════════════════════════════════════════════════════════════
FROM nginx:alpine AS runtime

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built Angular app
COPY --from=build /app/dist/FinTrustFDManager.UI/browser /usr/share/nginx/html

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
