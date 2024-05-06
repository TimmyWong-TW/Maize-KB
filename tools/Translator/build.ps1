docker build --target src -t translator:src -f Translator/Dockerfile .
# docker rm translator-build
docker run --name translator-build --gpus all translator:src
docker commit translator-build translator:build
# docker rm translator-build
docker compose build translator
# docker rmi translator:src translator:build