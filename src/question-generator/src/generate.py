import argparse, json, os, glob, re
from dotenv import load_dotenv

try:
    from .openai_helper import create_openai_client
except ImportError:  # pragma: no cover
    from openai_helper import create_openai_client

def read_file(p):
    with open(p, "r", encoding="utf-8") as f:
        return f.read()

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

def load_context(topic_or_group):
    ctx_dir = os.path.join(ROOT_DIR, "data", "contexts")
    mapping = {
        "NhanThucAI": "01_nhan_thuc_ai_co_ban.md",
        "DaoDucQuanTri": "02_quyen_rieng_tu.md",
        "KyThuatUngDung": "03_ung_dung_ai_quy_trinh.md",
        "ThietKeHeThong": "04_thiet_ke_co_trach_nhiem.md",
        "DanhGiaChatLuong": "05_danh_gia_chat_luong.md",
    }
    filename = mapping.get(topic_or_group, None)
    if filename:
        return read_file(os.path.join(ctx_dir, filename)), os.path.join(ctx_dir, filename)
    any_ctx = sorted(glob.glob(os.path.join(ctx_dir, "*.md")))
    if any_ctx:
        return read_file(any_ctx[0]), any_ctx[0]
    return "", ""

def fill_prompt_template(tmpl, values):
    """Replace simple {key} placeholders without touching other braces."""
    filled = tmpl
    for key, value in values.items():
        filled = filled.replace("{" + key + "}", str(value))
    return filled

def generate_questions(jobs, tmpl, client=None, model_name=None, debug=False):
    if not client:
        # Fallback if no client provided (though usually it should be)
        load_dotenv()
        base_url = os.getenv("OPENAI_BASE_URL", "http://localhost:8000/v1")
        base_url = base_url if base_url.endswith("/v1") else (base_url.rstrip("/") + "/v1")
        api_key = os.getenv("OPENAI_API_KEY", "EMPTY")
        client = create_openai_client(base_url=base_url, api_key=api_key)
    
    if not model_name:
        model_name = os.getenv("OPENAI_MODEL", "openai/gpt-oss-20b")

    questions = []
    skipped = 0

    for j in jobs:
        context_text, context_path = load_context(j["group"])
        prompt = fill_prompt_template(
            tmpl,
            {
                "group": j["group"],
                "level": j["level"],
                "type": j["type"],
                "difficulty": j["difficulty"],
                "topic": j.get("topic", ""),
                "context_text": context_text,
            },
        )

        messages = [
            {"role":"system","content":"Bạn là chuyên gia khảo thí, sinh câu hỏi theo schema JSON."},
            {"role":"user","content": prompt}
        ]
        try:
            resp = client.chat.completions.create(
                model=model_name,
                messages=messages,
                temperature=0.8,
                max_tokens=512,
            )
            txt = resp.choices[0].message.content.strip()
            if debug:
                print("RAW:", txt[:400])
            obj = None
            m = re.search(r"\{.*\}", txt, re.S)
            if m:
                try:
                    obj = json.loads(m.group(0))
                except Exception:
                    if debug:
                        print(f"[generate] job {j['id']} returned invalid JSON snippet.")
                    obj = None
            if not obj:
                skipped += 1
                print(f"[generate] job {j['id']} -> no valid JSON, skipping.")
                continue

            obj.setdefault("id", f"q_{j['id']}")
            if "source_context" not in obj or not obj["source_context"]:
                obj["source_context"] = context_path
            
            # Khắc phục tính ổn định cho mcq_single, mcq_multi (luôn có options và answer là A, B, C, D)
            if obj.get("type") in ["mcq_single", "mcq_multi"]:
                options = obj.get("options", [])
                if not isinstance(options, list) or len(options) == 0:
                    print(f"[generate] job {j['id']} -> options rỗng, bỏ qua (không hợp lệ cho {obj['type']}).")
                    skipped += 1
                    continue
                
                answers = obj.get("answer", [])
                if isinstance(answers, str):
                    answers = [answers]
                elif not isinstance(answers, list):
                    answers = list(answers)
                
                cleaned_ans = []
                for a in answers:
                    if isinstance(a, str):
                        a_clean = a.strip()
                        # Lấy chữ cái đầu nếu nó là A, B, C, D
                        if re.match(r"^[A-D](\.|\:|\s|$)", a_clean, re.IGNORECASE):
                            cleaned_ans.append(a_clean[0].upper())
                        else:
                            # Nếu có chữ cái nào đó riêng lẻ trong dãy
                            m_letter = re.search(r"\b([A-D])\b", a_clean.upper())
                            if m_letter:
                                cleaned_ans.append(m_letter.group(1))
                            else:
                                cleaned_ans.append(a_clean)
                
                # Lọc trùng lặp
                seen = set()
                dedup = []
                for ca in cleaned_ans:
                    ca_upper = str(ca).upper() if isinstance(ca, str) else ca
                    if ca_upper not in seen:
                        seen.add(ca_upper)
                        dedup.append(ca_upper)
                obj["answer"] = dedup
                
                if obj["type"] == "mcq_single" and len(obj["answer"]) > 1:
                    obj["answer"] = [obj["answer"][0]]

            questions.append(obj)

        except Exception as e:
            skipped += 1
            msg = f"[generate] job {j['id']} failed: {e}"
            print(msg)
            if debug:
                import traceback
                traceback.print_exc()
    
    return questions, skipped

def main():
    load_dotenv()
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", required=False, default=os.getenv("OPENAI_BASE_URL", "http://localhost:8000/v1"))
    ap.add_argument("--base-url", required=False, default=None)
    ap.add_argument("--model-name", required=False, default=os.getenv("OPENAI_MODEL", "openai/gpt-oss-20b"))
    ap.add_argument("--plan", required=True)
    ap.add_argument("--prompt", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--debug", action="store_true")
    args = ap.parse_args()

    base_url = args.base_url or args.model
    base_url = base_url if base_url.endswith("/v1") else (base_url.rstrip("/") + "/v1")
    api_key = os.getenv("OPENAI_API_KEY", "EMPTY")
    client = create_openai_client(base_url=base_url, api_key=api_key)

    with open(args.plan, "r", encoding="utf-8") as f:
        plan = json.load(f)
    tmpl = read_file(args.prompt)

    questions, skipped = generate_questions(plan["jobs"], tmpl, client=client, model_name=args.model_name, debug=args.debug)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "w", encoding="utf-8") as out_f:
        for q in questions:
            out_f.write(json.dumps(q, ensure_ascii=False) + "\n")

    if len(questions) == 0:
        raise RuntimeError("[generate] Không sinh được câu hỏi nào. Kiểm tra log lỗi ở trên hoặc chạy lại với --debug.")
    if skipped:
        print(f"[generate] cảnh báo: {skipped} job bị bỏ qua.")
    print(f"[generate] done -> {args.out}")

if __name__ == "__main__":
    main()
