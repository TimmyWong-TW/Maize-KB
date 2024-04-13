# Embedder

## 內嵌及關聯 Embeddings and Relevances

- 首次部署前先暫存模型。  
  Cache the models before first run.
  ```sh
  docker compose run --rm embedder-cache
  ```
  
- 保持服務在背景運行。  
  Keep the service up in the background.
  ```sh
  docker compose up -d embedder
  ```
  
- 曝露埠以供外部連接。  
  Expose ports for external use.
  ```sh
  docker compose -f docker-compose.yml -f docker-compose.override.yml up -d embedder
  ```