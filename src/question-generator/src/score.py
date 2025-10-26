\
import argparse, json, os, statistics

def clarity_penalty(stem):
    return 1.0 if len(stem) <= 240 else max(0.6, 1.0 - (len(stem)-240)/400.0)

def has_answer_leak(stem, answer):
    ans_str = " ".join(answer) if isinstance(answer, list) else str(answer)
    return ans_str.strip() and (ans_str.lower() in stem.lower())

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--report", required=True)
    args = ap.parse_args()

    scores = []
    details = []
    with open(args.inp, "r", encoding="utf-8") as f:
        for line in f:
            try:
                obj = json.loads(line)
            except Exception:
                continue
            s_schema = 1.0 if all(k in obj for k in ["stem","answer","type"]) else 0.6
            s_clarity = clarity_penalty(obj.get("stem",""))
            s_leak = 0.0 if has_answer_leak(obj.get("stem",""), obj.get("answer",[])) else 1.0
            s_total = 0.2*s_schema + 0.25*s_clarity + 0.25*s_leak + 0.15 + 0.15
            scores.append(s_total)
            details.append({"id": obj.get("id"), "score": round(s_total,3)})

    report = {
        "count": len(scores),
        "avg_score": round(statistics.mean(scores), 3) if scores else 0.0,
        "min": round(min(scores),3) if scores else 0.0,
        "max": round(max(scores),3) if scores else 0.0,
        "items": details[:50]
    }
    os.makedirs(os.path.dirname(args.report), exist_ok=True)
    with open(args.report, "w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    print(f"[score] report -> {args.report}")

if __name__ == "__main__":
    main()
