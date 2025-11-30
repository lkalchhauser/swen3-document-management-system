# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-eng \
    libtesseract-dev \
    libleptonica-dev \
    poppler-utils \
    libpoppler-cpp-dev \
    && rm -rf /var/lib/apt/lists/*

RUN find /usr/lib -name "liblept*" -type f && \
    LEPT_FILE=$(find /usr/lib/x86_64-linux-gnu -name "libleptonica.so*" -type f | head -n 1) && \
    if [ -n "$LEPT_FILE" ]; then \
        ln -sf $LEPT_FILE /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so; \
        ln -sf $LEPT_FILE /usr/lib/libleptonica-1.82.0.so; \
    fi

RUN tesseract --version && \
    mkdir -p /usr/share/tesseract-ocr/5/tessdata && \
    if [ -d /usr/share/tesseract-ocr/4.00/tessdata ] && [ ! -f /usr/share/tesseract-ocr/5/tessdata/eng.traineddata ]; then \
        cp -r /usr/share/tesseract-ocr/4.00/tessdata/* /usr/share/tesseract-ocr/5/tessdata/ 2>/dev/null || true; \
    fi && \
    ls -la /usr/share/tesseract-ocr/*/tessdata/ || true

USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DocumentManagementSystem.OcrWorker/DocumentManagementSystem.OcrWorker.csproj", "DocumentManagementSystem.OcrWorker/"]
RUN dotnet restore "./DocumentManagementSystem.OcrWorker/DocumentManagementSystem.OcrWorker.csproj"
COPY . .
WORKDIR "/src/DocumentManagementSystem.OcrWorker"
RUN dotnet build "./DocumentManagementSystem.OcrWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DocumentManagementSystem.OcrWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
USER root
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/runtimes/linux/native && \
    cp /usr/lib/x86_64-linux-gnu/libleptonica.so /app/runtimes/linux/native/libleptonica-1.82.0.so && \
    cp /usr/lib/x86_64-linux-gnu/libleptonica.so /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so && \
    cp /usr/lib/x86_64-linux-gnu/libleptonica.so /usr/lib/libleptonica-1.82.0.so && \
    chmod 755 /app/runtimes/linux/native/libleptonica-1.82.0.so && \
    echo "Copied libleptonica to multiple locations" && \
    ls -lh /app/runtimes/linux/native/libleptonica-1.82.0.so /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so

ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata
ENV LD_LIBRARY_PATH=/app/runtimes/linux/native:/usr/lib/x86_64-linux-gnu:/usr/lib:${LD_LIBRARY_PATH}

ENTRYPOINT ["dotnet", "DocumentManagementSystem.OcrWorker.dll"]