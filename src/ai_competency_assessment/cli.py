from __future__ import annotations

import argparse
import sys
from dataclasses import dataclass
from typing import Optional

import torch
from transformers import (
    AutoConfig,
    AutoModelForCausalLM,
    AutoModelForSeq2SeqLM,
    AutoTokenizer,
)


@dataclass
class GenerationConfig:
    model_name: str
    max_new_tokens: int
    temperature: Optional[float]
    top_p: Optional[float]
    device: str
    torch_dtype: Optional[torch.dtype]


def parse_args(argv: Optional[list[str]] = None) -> tuple[GenerationConfig, str]:
    parser = argparse.ArgumentParser(
        description="Run text generation with FLAN-T5 or other Hugging Face models."
    )
    parser.add_argument(
        "prompt",
        nargs="?",
        help="Prompt to feed into the model. If omitted, the program reads from stdin.",
    )
    parser.add_argument(
        "--model-name",
        default="google/flan-t5-small",
        help="Hugging Face model identifier to load. Defaults to google/flan-t5-small.",
    )
    parser.add_argument(
        "--max-new-tokens",
        type=int,
        default=128,
        help="Maximum number of new tokens to generate.",
    )
    parser.add_argument(
        "--temperature",
        type=float,
        default=None,
        help="Sampling temperature. Leave unset for greedy decoding.",
    )
    parser.add_argument(
        "--top-p",
        type=float,
        default=None,
        help="Nucleus sampling probability cutoff. Leave unset for greedy decoding.",
    )
    parser.add_argument(
        "--device",
        choices=("auto", "cpu", "cuda"),
        default="auto",
        help="Device to run inference on. Defaults to 'auto' (use CUDA when available).",
    )
    parser.add_argument(
        "--dtype",
        choices=("float32", "float16", "bfloat16"),
        default=None,
        help="Optional torch dtype for model weights (e.g. float16 for large models).",
    )
    args = parser.parse_args(argv)

    prompt = args.prompt
    if prompt is None:
        prompt = sys.stdin.read().strip()
        if not prompt:
            parser.error("No prompt provided. Supply a prompt argument or via stdin.")

    device = args.device
    if device == "auto":
        device = "cuda" if torch.cuda.is_available() else "cpu"

    if device == "cuda" and not torch.cuda.is_available():
        parser.error("CUDA requested but no GPU detected.")

    torch_dtype = None
    if args.dtype:
        dtype_map = {
            "float32": torch.float32,
            "float16": torch.float16,
            "bfloat16": torch.bfloat16,
        }
        torch_dtype = dtype_map[args.dtype]
        if device == "cpu" and torch_dtype == torch.float16:
            parser.error("float16 weights require CUDA; use --dtype float32 or --device cuda.")

    return GenerationConfig(
        model_name=args.model_name,
        max_new_tokens=args.max_new_tokens,
        temperature=args.temperature,
        top_p=args.top_p,
        device=device,
        torch_dtype=torch_dtype,
    ), prompt


def load_model(model_name: str, device: str, torch_dtype: Optional[torch.dtype]):
    config = AutoConfig.from_pretrained(model_name)
    tokenizer = AutoTokenizer.from_pretrained(model_name)

    if tokenizer.pad_token is None and tokenizer.eos_token is not None:
        tokenizer.pad_token = tokenizer.eos_token
        tokenizer.pad_token_id = tokenizer.eos_token_id

    model_cls = AutoModelForSeq2SeqLM if config.is_encoder_decoder else AutoModelForCausalLM
    model = model_cls.from_pretrained(model_name, torch_dtype=torch_dtype)

    if device == "cuda":
        model = model.to("cuda")

    return tokenizer, model, config


def generate_text(config: GenerationConfig, prompt: str) -> str:
    tokenizer, model, model_config = load_model(
        config.model_name, config.device, config.torch_dtype
    )

    inputs = tokenizer(prompt, return_tensors="pt")
    if config.device == "cuda":
        inputs = inputs.to("cuda")

    generate_kwargs = {"max_new_tokens": config.max_new_tokens}
    if config.temperature is not None or config.top_p is not None:
        # Switch to sampling when either temperature or top_p is provided.
        generate_kwargs["do_sample"] = True
        if config.temperature is not None:
            generate_kwargs["temperature"] = config.temperature
        if config.top_p is not None:
            generate_kwargs["top_p"] = config.top_p

    if not model_config.is_encoder_decoder and tokenizer.pad_token_id is not None:
        generate_kwargs.setdefault("pad_token_id", tokenizer.pad_token_id)

    output_ids = model.generate(**inputs, **generate_kwargs)
    result = tokenizer.decode(output_ids[0], skip_special_tokens=True)
    return result.strip()


def main(argv: Optional[list[str]] = None) -> int:
    try:
        config, prompt = parse_args(argv)
        completion = generate_text(config, prompt)
    except KeyboardInterrupt:
        return 130
    except SystemExit as exc:
        # argparse uses SystemExit for usage errors.
        return int(exc.code)
    except Exception as exc:  # pragma: no cover - surface unexpected issues.
        print(f"Error: {exc}", file=sys.stderr)
        return 1

    print(completion)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
