import argparse, json, os, re
from dotenv import load_dotenv

try:
    from .openai_helper import create_openai_client
except ImportError:  # pragma: no cover
    from openai_helper import create_openai_client

def main():
    load_dotenv()
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", required=False, default=os.getenv("OPENAI_BASE_URL", "http://localhost:8000/v1"))
    ap.add_argument("--base-url", required=False, default=None)
    ap.add_argument("--model-name", required=False, default=os.getenv("OPENAI_MODEL", "openai/gpt-oss-20b"))
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--prompt", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    base_url = args.base_url or args.model
    base_url = base_url if base_url.endswith("/v1") else (base_url.rstrip("/") + "/v1")
    api_key = os.getenv("OPENAI_API_KEY", "EMPTY")
    client = create_openai_client(base_url=base_url, api_key=api_key)

    with open(args.prompt, "r", encoding="utf-8") as f:
        verifier_prompt = f.read()

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    fout = open(args.out, "w", encoding="utf-8")

    kept = 0
    skipped = 0
    with open(args.inp, "r", encoding="utf-8") as fin:
        for line in fin:
            line = line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except Exception:
                skipped += 1
                print("[verify] bỏ qua 1 dòng input: JSON không hợp lệ.")
                continue

            content = json.dumps(obj, ensure_ascii=False, indent=2)
            messages = [
                {"role":"system","content":"Bạn là giám khảo thẩm định câu hỏi, chỉ trả về JSON hợp lệ hoặc {'reject':true}."},
                {"role":"user","content": verifier_prompt + "\n\nOBJECT:\n" + content}
            ]

            try:
                resp = client.chat.completions.create(
                    model=args.model_name,
                    messages=messages,
                    temperature=0.3,
                    max_tokens=400,
                )
                txt = resp.choices[0].message.content.strip()
                m = re.search(r"\{.*\}", txt, re.S)
                if not m:
                    skipped += 1
                    print(f"[verify] job {obj.get('id', 'unknown')} -> không tìm thấy JSON.")
                    continue
                try:
                    cand = json.loads(m.group(0))
                except Exception:
                    skipped += 1
                    print(f"[verify] job {obj.get('id', 'unknown')} -> JSON không hợp lệ.")
                    continue
                if cand.get("reject") is True:
                    skipped += 1
                    continue
                fout.write(json.dumps(cand, ensure_ascii=False) + "\n")
                kept += 1
            except Exception:
                skipped += 1
                print(f"[verify] job {obj.get('id', 'unknown')} thất bại.")
                continue

    fout.close()
    if kept == 0:
        raise RuntimeError("[verify] Không giữ lại câu hỏi nào. Kiểm tra log ở trên hoặc chạy với --debug.")
    if skipped:
        print(f"[verify] cảnh báo: {skipped} mục bị loại.")
    print(f"[verify] done -> {args.out}")

if __name__ == "__main__":
    main()
