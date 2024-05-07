FROM pytorch/pytorch:2.3.0-cuda12.1-cudnn8-runtime
WORKDIR /app
EXPOSE 80
RUN pip3 install gunicorn flask transformers gevent
COPY Translator/main.py ./
CMD gunicorn -w 1 -k gevent main:app --bind 0.0.0.0:80