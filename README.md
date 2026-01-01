# MediaTrust – Distributed Media Analysis Platform

## Overview

**MediaTrust** is a distributed, microservices-based platform for uploading, analyzing,
and reporting on media files.  
The system demonstrates service isolation, message-driven orchestration,
controlled execution, and read-optimized reporting.

All services are containerized and communicate via HTTP and RabbitMQ.

---

## Services

### Gateway (API Gateway)
**Responsibilities**
- Single public entry point for all clients
- HTTP reverse proxy using YARP
- Centralized routing to internal services
- No business logic

**Routes**
- `/media/*` → Ingest
- `/jobs/*` → Orchestrator
- `/detectors/*` → Detectors
- `/reports/*` → Report

---

### Ingest Service
**Responsibilities**
- Accepts media uploads via HTTP
- Stores raw files in MinIO
- Persists media metadata in PostgreSQL
- Triggers analysis job creation in Orchestrator

**Key Behavior**
- Validates upload requests
- Prevents duplicate uploads (by filename)
- Acts as the system entry point for media

**Storage**
- MinIO (media files)

**Database**
- PostgreSQL (media metadata)

---

### Orchestrator Service
**Responsibilities**
- Manages the lifecycle of analysis jobs
- Creates jobs when new media is uploaded
- Tracks job status:
  - `Pending`
  - `Processing`
  - `Completed`
  - `Failed`
- Publishes detector tasks to RabbitMQ

**Key Behavior**
- Pure orchestration logic
- No detector execution
- No media access

**Database**
- PostgreSQL (analysis jobs)

---

### Detectors Service
**Responsibilities**
- Executes detector logic
- Processes **exactly one message per request**
- Stores detector results
- Updates job status through Gateway

**Execution Model**
- Pull-based
- No background consumers
- No automatic retries
- Deterministic and operator-controlled execution

**Why Pull-Based**
- Full operational control
- Easy debugging
- No hidden background activity
- Safer execution in regulated systems

**Endpoint**
- POST /detectors/run-once

**Database**
- PostgreSQL (detector results)

---

### Report Service
**Responsibilities**
- Read-only reporting service
- Aggregates detector results
- Computes overall risk score
- Produces final media report

**Key Behavior**
- No writes
- No job execution
- Pure read and aggregation logic

**Endpoint**
- GET /reports/media/{mediaId}

**Database**
- PostgreSQL (read-only access to detector results)

---

## Architecture

            Client
            ↓
            Gateway
            ↓
            Ingest
            ↓
            Orchestrator
            ↓
            RabbitMQ
            ↓
            Detectors
            ↓
            PostgreSQL
            ↓
            Report

---

## End-to-End Flow

### 1. Upload Media
**POST /media**
- File is stored in MinIO
- Metadata is stored in PostgreSQL
- Orchestrator creates a new job
- Job status = `Pending`

---

### 2. Job Creation
- Orchestrator persists the job
- Orchestrator publishes a detector task to RabbitMQ

---

### 3. Detector Execution
**POST /detectors/run-once**
- Detector pulls **one** message from RabbitMQ
- Job status updated to `Processing`
- Detector logic is executed
- Detector result is stored
- Job status updated to `Completed` or `Failed`

---

### 4. Reporting
**GET /reports/media/{mediaId}**
- Report service reads detector results
- Computes risk score
- Returns aggregated report

---

## Job Status Lifecycle

            | Status       | Meaning                          |
            |--------------|----------------------------------|
            | Pending      | Job created, not processed       |
            | Processing   | Detector currently running       |
            | Completed    | Detector finished successfully   |
            | Failed       | Detector execution failed        |

---

## Tech Stack

- .NET 10
- ASP.NET Core
- PostgreSQL
- RabbitMQ
- YARP Reverse Proxy
- MinIO
- Docker & Docker Compose

---

## How to Run

            docker compose up --build

### Gateway will be available at:

            http://localhost:8080


### Example API Usage

**Upload Media**
- POST http://localhost:8080/media

**Run Detector**
- POST http://localhost:8080/detectors/run-once

**Get Job Status**
- GET http://localhost:8080/jobs

**Get Media Report**
- GET http://localhost:8080/reports/media/{mediaId}