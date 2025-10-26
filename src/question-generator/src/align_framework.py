\
import argparse, json, os

try:
    import yaml
except ImportError:
    yaml = None

def align_with_framework(item, mapping):
    group = item.get("group")
    if group not in mapping:
        return False
    text_bits = []
    text_bits.append(item.get("stem",""))
    opts = item.get("options", [])
    if isinstance(opts, list):
        text_bits.extend(opts)
    text = " ".join(text_bits).lower()
    keywords = [k.lower() for k in mapping[group].get("keywords",[])]
    return any(k in text for k in keywords)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--mapping", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    if yaml is None:
        raise RuntimeError("Thiếu thư viện PyYAML. Cài bằng: pip install pyyaml")

    with open(args.mapping, "r", encoding="utf-8") as f:
        mapping = yaml.safe_load(f)

    aligned, rejected = [], []
    with open(args.inp, "r", encoding="utf-8") as fin:
        for line in fin:
            try:
                obj = json.loads(line)
            except Exception:
                continue
            if align_with_framework(obj, mapping):
                aligned.append(obj)
            else:
                rejected.append(obj)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "w", encoding="utf-8") as f:
        for it in aligned:
            f.write(json.dumps(it, ensure_ascii=False)+"\n")

    report = {
        "total": len(aligned)+len(rejected),
        "aligned": len(aligned),
        "rejected": len(rejected),
        "rate": round(len(aligned)/(len(aligned)+len(rejected)+1e-9),3)
    }
    rep_path = os.path.join(os.path.dirname(args.out), "../reports/alignment.json")
    os.makedirs(os.path.dirname(rep_path), exist_ok=True)
    with open(rep_path, "w", encoding="utf-8") as rf:
        json.dump(report, rf, ensure_ascii=False, indent=2)

    print(f"[align] done: {len(aligned)} kept, {len(rejected)} rejected -> {args.out}")

if __name__ == "__main__":
    main()
