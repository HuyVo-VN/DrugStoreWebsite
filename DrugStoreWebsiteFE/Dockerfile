# ===== Stage 1: Build Angular =====
FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm install -g @angular/cli
RUN npm install

COPY . .

RUN npx ng build Authentication --configuration=production


# ===== Stage 2: Nginx =====
FROM nginx:alpine

# Xóa config mặc định
RUN rm /etc/nginx/conf.d/default.conf

# Copy config mới
COPY default.conf /etc/nginx/conf.d/default.conf

# Copy build Angular
COPY --from=build /app/dist/Authentication/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
