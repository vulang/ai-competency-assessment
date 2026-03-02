AI Competency Assessment Architecture
=====================================

This document captures a textual version of the provided architecture diagram so it can
be reviewed alongside the implementation.

Frontend (Angular)
------------------
- Candidate web app and admin portal.
- HTTPS + WSS (SignalR) for realtime chat/monitoring.
- Auth tokens issued by the identity service, validated at the API gateway.

API Gateway + Security
----------------------
- TLS termination, routing, and auth token validation.
- Optional rate limits and request logging.

Backend Services (.NET / Minimal APIs)
--------------------------------------
- Identity service: user management, roles, token issuance.
- Competency core service: frameworks, domains, skills, criteria.
- Content management service: question bank and attachments.
- Reporting & analytics service: exam outcomes, score distribution.
- Exam sandbox service: orchestrates sandbox runs, collects logs, and pushes
  events for grading.

AI Grading Engine
-----------------
- LLM-as-a-judge logic orchestrated by Semantic Kernel functions.
- Receives candidate answers (and sandbox logs when applicable).
- Emits structured grading results and feedback.

Sandbox Isolation
-----------------
- Executes candidate code in containerized sandboxes.
- Streams stdout/stderr and runtime metrics to the backend.

AI Integration Service
----------------------
- Central integration layer for model routing and prompt orchestration.
- Talks to external LLM APIs (OpenAI, Gemini, Claude) or internal models.

Async Messaging
--------------
- Message queue for grading jobs and long-running tasks.

Data Layer
----------
- Primary relational store (PostgreSQL) for structured entities.
- Interaction store (MongoDB) for chat and session traces.
- Vector store (pgvector/Qdrant/Pinecone) for semantic retrieval.
- Object storage (S3/MinIO/Azure Blob) for submissions and artifacts.

Key Flows
---------
- Candidate starts exam -> session created -> answers stored -> grading job queued.
- Sandbox questions run in containers -> logs stored -> grading engine uses logs.
- Admin manages frameworks, domains, skills, questions, and exam composition.
