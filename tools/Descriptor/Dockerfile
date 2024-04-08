FROM node:lts-alpine
WORKDIR /app
RUN npm i glob @google/generative-ai
COPY Descriptor/describe.mjs ./
ENTRYPOINT [ "node", "describe.mjs" ]