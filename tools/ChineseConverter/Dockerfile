FROM node:lts-alpine
RUN apk add python3 make g++
WORKDIR /app
RUN npm i glob opencc
COPY ChineseConverter/json2tsv.mjs ./
ENTRYPOINT [ "node", "json2tsv.mjs" ]