FROM pytorch/pytorch:2.2.2-cuda12.1-cudnn8-runtime
WORKDIR /app
EXPOSE 80
RUN pip3 install flask sentence_transformers
RUN pip3 install gunicorn
COPY SentenceTransformer/main.py ./
CMD gunicorn -w $(nproc) main:app --bind 0.0.0.0:80