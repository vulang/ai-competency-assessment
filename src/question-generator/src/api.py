from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional, Any, Dict
import sys
import os
import json
import asyncio
from dotenv import load_dotenv

# Add current directory to path to import src modules if needed
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

try:
    from .plan import expand_plan
    from .generate import generate_questions
except ImportError:
    from plan import expand_plan
    from generate import generate_questions

load_dotenv()

from fastapi.middleware.cors import CORSMiddleware

app = FastAPI(title="Question Generator API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:4200"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class MixItem(BaseModel):
    group: str
    level: str
    type: str
    difficulty: List[int]
    topic: str = ""
    count: int = 1

class GenerationPlanRequest(BaseModel):
    total: int = 100
    mix: List[MixItem]

class GenerationRequest(BaseModel):
    topic: str
    count: int = 5
    difficulty: Optional[str] = "medium"

@app.post("/generate-from-plan", response_model=List[Dict[str, Any]])
async def generate_from_plan(request: GenerationPlanRequest):
    try:
        # Load default prompt template
        # Assuming run from src or similar structure, finding prompt relative to api.py
        current_dir = os.path.dirname(os.path.abspath(__file__))
        project_root = os.path.dirname(current_dir)
        prompt_path = os.path.join(project_root, "prompts", "generator_vn.txt")
        
        if not os.path.exists(prompt_path):
            raise HTTPException(status_code=500, detail=f"Prompt template not found at {prompt_path}")

        with open(prompt_path, "r", encoding="utf-8") as f:
            prompt_tmpl = f.read()

        # Convert Pydantic model to dict for expand_plan
        plan_dict = request.model_dump()
        
        # Expand plan into jobs
        jobs = expand_plan(plan_dict)
        
        # Generate questions
        # Note: generate_questions is synchronous in current implementation (calls openai sync)
        # If necessary to be async, we would need to wrap it or use async client.
        # For now, running valid sync code.
        questions, skipped = generate_questions(jobs, prompt_tmpl)
        
        if not questions and skipped > 0:
             raise HTTPException(status_code=500, detail="Failed to generate any questions (all skipped). Check server logs.")

        return questions

    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/generate")
async def generate_questions_simple(request: GenerationRequest):
    # Backward compatibility or simple endpoint
    # Create a simple plan from the request
    # This maps the simple request to the complex plan structure
    
    # We need to map 'difficulty' str to int list, etc.
    # This is just a mock mapping for demonstration
    diff_map = {"easy": [1], "medium": [2], "hard": [3]}
    difficulty = diff_map.get(request.difficulty, [2])
    
    mix_item = MixItem(
        group="NhanThucAI", # Default group
        level="NenTang",      # Default level
        type="mcq_single",    # Default type
        difficulty=difficulty,
        topic=request.topic,
        count=request.count
    )
    
    plan = GenerationPlanRequest(total=request.count, mix=[mix_item])
    return await generate_from_plan(plan)

@app.get("/health")
def health_check():
    return {"status": "healthy"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
