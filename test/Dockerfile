FROM debian:bullseye-slim

WORKDIR /app

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    postgresql-client \
    bash \
    && rm -rf /var/lib/apt/lists/*

COPY wait-for-it.sh /usr/local/bin/wait-for-it
COPY init-fixtures.sql init-fixtures-bookings.sql regress.sh ./
RUN chmod +x /usr/local/bin/wait-for-it regress.sh

# Fix line endings for Windows compatibility
RUN sed -i 's/\r$//' regress.sh

ENTRYPOINT ["./regress.sh"]
