FROM pytorch/pytorch:2.2.2-cuda12.1-cudnn8-runtime
RUN apt-get update && apt-get install -y git && apt-get clean && rm -rf /var/lib/apt/lists/*
RUN pip install git+https://github.com/m-bain/whisperx.git
VOLUME /root/.cache
