FROM nvidia/cuda:12.3.2-runtime-ubuntu22.04 AS base

FROM nvidia/cuda:12.3.2-devel-ubuntu22.04 AS src
WORKDIR /src
ADD https://static.rust-lang.org/rustup/dist/x86_64-unknown-linux-gnu/rustup-init rustup-init
RUN chmod +x rustup-init && ./rustup-init -qy --profile minimal --no-modify-path && ln -s /root/.cargo/bin/cargo /usr/bin/cargo
RUN apt-get update && apt-get install -y git pkg-config libssl-dev && apt-get clean
RUN git clone --depth 1 https://github.com/huggingface/candle.git
WORKDIR /src/candle
ENTRYPOINT cargo build --example t5 -F cuda -r

FROM translator:build AS build

FROM base AS publish
WORKDIR /usr/local/cuda/lib64
RUN ln -s libcublas.so.12 libcublas.so && \
    ln -s libcurand.so.10 libcurand.so && \
    ln -s libOpenCL.so.1.0 libOpenCL.so && \
    ln -s libcublasLt.so.12 libcublasLt.so && \
    ln -s libcudart.so.12 libcudart.so && \
    ln -s libcufft.so.11 libcufft.so && \
    ln -s libcufftw.so.11 libcufftw.so && \
    ln -s libcufile.so.0 libcufile.so && \
    ln -s libcufile_rdma.so.1 libcufile_rdma.so && \
    ln -s libcusolver.so.11 libcusolver.so && \
    ln -s libcusolverMg.so.11 libcusolverMg.so && \
    ln -s libcusparse.so.12 libcusparse.so && \
    ln -s libnppc.so.12 libnppc.so && \
    ln -s libnppial.so.12 libnppial.so && \
    ln -s libnppif.so.12 libnppif.so && \
    ln -s libnppig.so.12 libnppig.so && \
    ln -s libnppim.so.12 libnppim.so && \
    ln -s libnppist.so.12 libnppist.so && \
    ln -s libnppisu.so.12 libnppisu.so && \
    ln -s libnppitc.so.12 libnppitc.so && \
    ln -s libnpps.so.12 libnpps.so && \
    ln -s libnvJitLink.so.12 libnvJitLink.so && \
    ln -s libnvblas.so.12 libnvblas.so && \
    ln -s libnvjpeg.so.12 libnvjpeg.so && \
    ln -s libnvrtc-builtins.so.12.3 libnvrtc-builtins.so && \
    ln -s libnvrtc.so.12 libnvrtc.so
WORKDIR /app
COPY --from=build /src/candle/target/release/examples/t5 t5
ENTRYPOINT [ "/app/t5", "--model-id", "google/madlad400-3b-mt", "--decode", "--temperature", "0" ]
