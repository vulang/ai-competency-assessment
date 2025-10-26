\
import argparse, json, os, random

try:
    import yaml
except ImportError:
    yaml = None

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--config", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    with open(args.config, "r", encoding="utf-8") as f:
        if args.config.endswith(".yaml") or args.config.endswith(".yml"):
            if yaml is None:
                raise RuntimeError("PyYAML chưa được cài. pip install pyyaml")
            plan = yaml.safe_load(f)
        else:
            plan = json.load(f)

    jobs = []
    jid = 0
    for mix in plan.get("mix", []):
        count = mix.get("count", 1)
        for _ in range(count):
            jid += 1
            job = {
                "id": f"job_{jid:04d}",
                "group": mix["group"],
                "level": mix["level"],
                "type": mix["type"],
                "difficulty": random.choice(mix["difficulty"]),
                "topic": mix.get("topic", ""),
            }
            jobs.append(job)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "w", encoding="utf-8") as f:
        json.dump({"total": len(jobs), "jobs": jobs}, f, ensure_ascii=False, indent=2)

    print(f"[plan] wrote {len(jobs)} jobs to {args.out}")

if __name__ == "__main__":
    main()
