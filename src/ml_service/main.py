"""
AI Competency Assessment — ML Microservice
==========================================
FastAPI service exposing IRT, BKT, and Competency Profile endpoints.
Run with: uvicorn main:app --host 0.0.0.0 --port 8001 --reload
"""
import sys
import os

# Ensure local modules are importable
sys.path.insert(0, os.path.dirname(__file__))

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from routers.irt_router import router as irt_router
from routers.kt_router import router as kt_router
from routers.profile_router import router as profile_router

app = FastAPI(
    title="AI Competency ML Service",
    description=(
        "Machine Learning microservice for IRT-based ability estimation, "
        "Bayesian Knowledge Tracing, and Competency Profile generation."
    ),
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Restrict in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(irt_router)
app.include_router(kt_router)
app.include_router(profile_router)


@app.get("/health", tags=["Health"])
async def health_check():
    return {"status": "ok", "service": "ml-service", "version": "1.0.0"}
