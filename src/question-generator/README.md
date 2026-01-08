# TwinStar-QG POC (GPT-OSS-20B, vLLM, Vietnamese)

POC sinh câu hỏi tự động theo kiến trúc TwinStar (Generator + Verifier + Align) chạy trên vLLM với openai/gpt-oss-20b (MXFP4).

## 1) Khởi chạy model
```bash
docker compose up -d
export OPENAI_BASE_URL=http://localhost:8000/v1
export OPENAI_API_KEY=EMPTY
export OPENAI_MODEL=openai/gpt-oss-20b
```

## 1b) Chạy với hosted LLM (vd. OpenAI API)
```bash
export OPENAI_BASE_URL=https://api.openai.com/v1
export OPENAI_API_KEY=YOUR_OPENAI_API_KEY
export OPENAI_MODEL=gpt-4o-mini
```

## 1c) Tuỳ chỉnh base URL (proxy/hosted khác)
```bash
export OPENAI_BASE_URL=https://your-proxy.example.com/v1
export OPENAI_API_KEY=YOUR_API_KEY
export OPENAI_MODEL=your-model-name
```

## 2) Pipeline sinh câu hỏi
```bash
python -m src.plan --config configs/plan.yaml --out .cache/plan.json

python -m src.generate   --model http://localhost:8000/v1   --model-name openai/gpt-oss-20b   --plan .cache/plan.json   --prompt prompts/generator_vn.txt   --out .cache/gen.jsonl

python -m src.verify   --model http://localhost:8000/v1   --model-name openai/gpt-oss-20b   --in .cache/gen.jsonl   --prompt prompts/verifier_vn.txt   --out .cache/refined.jsonl

python -m src.align_framework   --in .cache/refined.jsonl   --mapping data/framework_mapping.yaml   --out .cache/aligned.jsonl

python -m src.score --in .cache/aligned.jsonl --report reports/quality.json

python -m src.export --in .cache/aligned.jsonl --csv out/questions.csv --jsonl out/questions.jsonl
```

Ghi chú: có thể dùng `--base-url` để override base URL khi chạy `src.generate`/`src.verify` (ví dụ: `--base-url https://your-proxy.example.com/v1`).

Ví dụ chạy với OpenAI API (base URL + tham số liên quan):
```bash
export OPENAI_BASE_URL=https://api.openai.com/v1
export OPENAI_API_KEY=YOUR_OPENAI_API_KEY
export OPENAI_MODEL=gpt-4o-mini

python -m src.generate   --base-url https://api.openai.com/v1   --model-name gpt-4o-mini   --plan .cache/plan.json   --prompt prompts/generator_vn.txt   --out .cache/gen.jsonl

python -m src.verify   --base-url https://api.openai.com/v1   --model-name gpt-4o-mini   --in .cache/gen.jsonl   --prompt prompts/verifier_vn.txt   --out .cache/refined.jsonl
```
