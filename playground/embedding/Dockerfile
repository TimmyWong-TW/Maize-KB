FROM pytorch/pytorch:2.3.0-cuda12.1-cudnn8-runtime AS train
WORKDIR /app
EXPOSE 80
RUN pip3 install gunicorn flask gevent sentence-transformers scikit-learn
COPY embedding/main.py ./
ENTRYPOINT python main.py

FROM python:slim AS inference
WORKDIR /app
EXPOSE 80
RUN pip3 install torch --index-url https://download.pytorch.org/whl/cpu
RUN pip3 install gunicorn flask gevent sentence-transformers scikit-learn
COPY embedding/main.py ./
CMD gunicorn -k gevent -w 1 main:app --bind 0.0.0.0:80 --timeout 30
