# TwinStar-QG POC (GPT-OSS-20B, vLLM, Vietnamese)

POC sinh câu hỏi tự động theo kiến trúc TwinStar (Generator + Verifier + Align) chạy trên vLLM với openai/gpt-oss-20b (MXFP4).

## 1) Khởi chạy model
```bash
docker compose up -d
export OPENAI_BASE_URL=http://localhost:8000/v1
export OPENAI_API_KEY=EMPTY
```

## 2) Pipeline sinh câu hỏi
```bash
python -m src.plan --config configs/plan.yaml --out .cache/plan.json

python -m src.generate   --model http://localhost:8000/v1   --plan .cache/plan.json   --prompt prompts/generator_vn.txt   --out .cache/gen.jsonl

python -m src.verify   --model http://localhost:8000/v1   --in .cache/gen.jsonl   --prompt prompts/verifier_vn.txt   --out .cache/refined.jsonl

python -m src.align_framework   --in .cache/refined.jsonl   --mapping data/framework_mapping.yaml   --out .cache/aligned.jsonl

python -m src.score --in .cache/aligned.jsonl --report reports/quality.json

python -m src.export --in .cache/aligned.jsonl --csv out/questions.csv --jsonl out/questions.jsonl
```
