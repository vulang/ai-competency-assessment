FROM golang:1.24-alpine AS build
RUN apk add --no-cache git
WORKDIR /source
# Build the community release from source; its public images are unavailable.
RUN git clone --depth 1 --branch RELEASE.2025-10-15T17-29-55Z https://github.com/minio/minio.git .
RUN CGO_ENABLED=0 go build -p 2 -trimpath -o /out/minio .

FROM alpine:3.22
RUN apk add --no-cache ca-certificates && mkdir /data
COPY --from=build /out/minio /usr/local/bin/minio
COPY --from=build /source/LICENSE /usr/share/licenses/minio/LICENSE
LABEL org.opencontainers.image.source="https://github.com/minio/minio/tree/RELEASE.2025-10-15T17-29-55Z"
EXPOSE 9000 9001
VOLUME /data
HEALTHCHECK --interval=5s --timeout=3s --start-period=20s CMD wget -q -O /dev/null http://127.0.0.1:9000/minio/health/live || exit 1
ENTRYPOINT ["minio"]
CMD ["server", "/data", "--console-address", ":9001"]
