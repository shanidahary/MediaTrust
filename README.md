# MediaTrust – Distributed Media Analysis Platform

## Overview
MediaTrust is a microservices-based system for uploading, processing,
and analyzing media files using HTTP and RabbitMQ.

## Services

### Ingest
- Uploads files
- Stores data in MinIO and Postgres
- Triggers job creation

### Orchestrator
- Manages analysis jobs
- Publishes tasks to RabbitMQ

### Detectors
- Pull-based execution
- Processes one message per request
- Stores analysis results

## Architecture
Client → Ingest → Orchestrator → RabbitMQ → Detectors

## Why pull-based detectors?
- Deterministic execution
- Operational control
- Safe retries
- No hidden background workers

## Tech Stack
- .NET 10
- ASP.NET Core
- PostgreSQL
- RabbitMQ
- Docker / Docker Compose
- MinIO

## How to run

docker compose up --build
