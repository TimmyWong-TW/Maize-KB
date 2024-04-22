FROM node:lts-alpine
RUN apk add python3 make g++
WORKDIR /app
RUN npm i glob hanzi-tools
COPY ChineseConverter/convert.mjs ./
ENTRYPOINT [ "node", "convert.mjs" ]