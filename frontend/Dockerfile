# Estágio 1: Build
FROM node:18-alpine as build

WORKDIR /app

# Copia os arquivos de dependência
COPY package*.json ./

# Instala as dependências
RUN npm install

# Copia o resto do projeto
COPY . .

# Recebe a URL da API como argumento de build (necessário para o Vite "imprimir" a variavel no JS)
ARG GENERAL_BASEURL
ENV VITE_GENERAL_BASEURL=$GENERAL_BASEURL

# Gera a pasta 'dist' otimizada para produção
RUN npm run build
# Estágio 2: Servidor Web (Nginx)
FROM nginx:alpine

# 1. Copia o build do React
COPY --from=build /app/dist /usr/share/nginx/html

# 2. COPIA O SEU NGINX.CONF PERSONALIZADO (Essa é a novidade)
# Substitui a configuração padrão do Nginx pela sua
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]