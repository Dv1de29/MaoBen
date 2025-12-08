# Etapa 1: Build Node.js
FROM node:22 as build
WORKDIR /app
COPY package.json .
RUN npm install
COPY . .
RUN npm run build 

# Etapa 2: Server Nginx (pentru producție/docker)
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]