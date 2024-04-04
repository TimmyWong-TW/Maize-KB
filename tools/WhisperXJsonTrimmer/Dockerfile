FROM node:lts-alpine
WORKDIR /app
RUN npm i glob
COPY WhisperXJsonTrimmer/trim.mjs ./
ENTRYPOINT [ "node", "trim.mjs" ]