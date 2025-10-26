# ai-competency-assessment

Simple Python project that runs FLAN-T5 and compatible Hugging Face models (e.g. VinAI's PhoGPT) for quick text generation experiments.

## Prerequisites
- Python 3.9 or newer
- `pip` (or another PEP 517 installer such as `pipx`)
- Sufficient disk space for the chosen FLAN-T5 model (the default `google/flan-t5-small` downloads ~1 GB on first run)

## Installation
```bash
python -m venv .venv
source .venv/bin/activate
pip install --upgrade pip
pip install -e .
```

The first invocation will download the model weights and tokenizer files into your local Hugging Face cache.

## Usage
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
- `--model-name vinai/PhoGPT-4B --dtype float16` to run VinAI's PhoGPT model (requires significant GPU VRAM)

## Development
This project uses a simple src-based layout. Edit code under `src/ai_competency_assessment/` and re-install with `pip install -e .` if you add dependencies.
