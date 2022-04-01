FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
WORKDIR /src
ADD ./Ocr.csproj ./Ocr.csproj
RUN dotnet restore "./Ocr.csproj"

ADD . .
RUN dotnet publish -c Release -o /release -r linux-x64 --self-contained

# ---

FROM archlinux

RUN pacman -Sy --noconfirm tesseract tesseract-data-deu && \
    rm -rf /var/cache/pacman/pkg/* && \
    rm -rf /var/lib/pacman/sync/*

WORKDIR /app
COPY --from=builder /release /app

CMD ["./Ocr"]