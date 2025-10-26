\
import argparse, json, csv, os

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--csv", required=True)
    ap.add_argument("--jsonl", required=True)
    args = ap.parse_args()

    items = []
    with open(args.inp, "r", encoding="utf-8") as f:
        for line in f:
            try:
                items.append(json.loads(line))
            except Exception:
                continue

    os.makedirs(os.path.dirname(args.csv), exist_ok=True)
    fields = ["id","lang","group","level","topic","type","difficulty","stem","options","answer","bloom","tags","source_context"]
    with open(args.csv, "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader()
        for it in items:
            row = {k: it.get(k,"") for k in fields}
            if isinstance(row.get("options"), list):
                row["options"] = " | ".join(row["options"])
            if isinstance(row.get("answer"), list):
                row["answer"] = " | ".join(row["answer"])
            if isinstance(row.get("tags"), list):
                row["tags"] = " | ".join(row["tags"])
            w.writerow(row)

    with open(args.jsonl, "w", encoding="utf-8") as f:
        for it in items:
            f.write(json.dumps(it, ensure_ascii=False)+"\n")

    print(f"[export] wrote CSV -> {args.csv}")
    print(f"[export] wrote JSONL -> {args.jsonl}")

if __name__ == "__main__":
    main()
