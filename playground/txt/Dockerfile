FROM node:lts-alpine
WORKDIR /app
RUN npm i glob
COPY txt/convert.mjs ./
ENTRYPOINT [ "node", "convert.mjs" ]