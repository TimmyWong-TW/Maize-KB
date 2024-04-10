FROM python:slim
WORKDIR /app
EXPOSE 80
RUN pip3 install torch --index-url https://download.pytorch.org/whl/cpu
RUN pip3 install gunicorn flask FlagEmbedding
COPY Embedder/main.py ./
CMD gunicorn -w 1 main:app --bind 0.0.0.0:80