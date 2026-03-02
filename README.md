# AI Competency Assessment

A full-stack application for assessing AI competency, featuring a .NET backend, Angular frontend, and various AI integration points.

## System design assets
- Architecture overview: `docs/architecture.md`
- PostgreSQL schema (ERD implementation): `db/schema.sql`
- Angular + Bootstrap UI scaffold: `frontend/ai-competency-ui`
- .NET API scaffold: `backend/AiCompetency.Api`
- Infrastructure compose file: `infra/docker-compose.yml`

## Prerequisites
- **Docker & Docker Compose**: For running infrastructure services (Postgres, Mongo, RabbitMQ, etc.).
- **.NET 8.0 SDK** (or compatible): For running the backend API.
- **Node.js** (LTS recommended) & **npm**: For running the Angular frontend.
- **Python 3.9+**: For the stand-alone question generator POC.

## Running the Application

### 1. Infrastructure
Start the required databases and services:

```bash
cd infra
docker compose up -d
```
This starts PostgreSQL (port 5434), MongoDB, RabbitMQ, MinIO, Qdrant, and the Question Generator service (port 5001).

> **Note**: The root `docker-compose.yml` is for the legacy stand-alone POC and should not be used when running the full stack.

### 2. Backend API
Run the .NET API:

```bash
cd backend/AiCompetency.Api
dotnet run
```
The API will typically start on `http://localhost:5000` (HTTP) or `https://localhost:5001` (HTTPS).

### 3. Frontend UI
Run the Angular application:

```bash
cd frontend/ai-competency-ui
npm install # Only needed the first time
npm start
```
Access the application at `http://localhost:4200`.

### 4. Default Login
- **Username**: `admin@example.com`
- **Password**: `Password123!`

---

## Stand-alone Question Generator POC
(Legacy/Experimental Python Tool)

Simple Python project that runs FLAN-T5 and compatible Hugging Face models (e.g. VinAI's PhoGPT) for quick text generation experiments.

### Python Prerequisites
- Python 3.9 or newer
- `pip`
- Sufficient disk space for model weights

### Installation
```bash
python -m venv .venv
source .venv/bin/activate
pip install --upgrade pip
pip install -e .
```

### Usage
Run the CLI with an inline prompt:
```bash
run-flan "Translate to French: How are you today?"
```

Or pipe text in:
```bash
echo "Summarize: The rain in Spain stays mainly in the plain." | run-flan
```

Additional options:
- `--model-name google/flan-t5-base` to pick a different FLAN-T5 checkpoint
- `--max-new-tokens 64` to control response length
- `--temperature 0.7 --top-p 0.9` to enable sampling
- `--device cuda` to force GPU usage when available
- `--model-name vinai/PhoGPT-4B --dtype float16` to run VinAI's PhoGPT model

## Development
This project uses a simple src-based layout. Edit code under `src/ai_competency_assessment/` and re-install with `pip install -e .` if you add dependencies.
