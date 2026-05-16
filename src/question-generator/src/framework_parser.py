import os
import re
from typing import Dict, Any, Optional

def _normalize_group_name(group: str) -> str:
    """Normalize English group names from API to the Vietnamese names in the markdown if needed, 
    or just match the English name in parentheses."""
    return group.lower().strip()

def _normalize_level_name(level: str) -> str:
    return level.lower().strip()

class FrameworkParser:
    def __init__(self, framework_path: str, mcq_examples_path: str, sit_examples_path: str):
        self.framework_path = framework_path
        self.mcq_examples_path = mcq_examples_path
        self.sit_examples_path = sit_examples_path
        
        self.framework_data = self._parse_framework()
        self.mcq_examples = self._parse_examples(self.mcq_examples_path)
        self.sit_examples = self._parse_examples(self.sit_examples_path)

    def _parse_framework(self) -> Dict[str, Dict[str, Dict[str, str]]]:
        data = {}
        if not os.path.exists(self.framework_path):
            return data
            
        with open(self.framework_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # Split by Tiêu chí
        criteria_blocks = re.split(r'\n## Tiêu chí \d+:', content)
        for block in criteria_blocks[1:]:
            # Extract group name from header, e.g. " Nền tảng AI (Fundamental)"
            header_match = re.search(r'^(.*?)\n', block)
            if not header_match:
                continue
            header = header_match.group(1)
            group_en_match = re.search(r'\((.*?)\)', header)
            if not group_en_match:
                continue
            group_en = group_en_match.group(1).strip()
            
            data[group_en] = {}
            
            # Extract definition (it's at the criterion level, so applies to all levels)
            definition = ""
            def_match = re.search(r'\*\*Định nghĩa\*\*:\s*(.*?)\n', block)
            if def_match:
                definition = def_match.group(1).strip()

            # Split by Mức
            level_blocks = re.split(r'\n### Mức ', block)
            for lblock in level_blocks[1:]:
                # Extract level name, e.g. "Cơ bản (Basic)"
                lheader_match = re.search(r'^(.*?)\n', lblock)
                if not lheader_match:
                    continue
                lheader = lheader_match.group(1)
                level_en_match = re.search(r'\((.*?)\)', lheader)
                if not level_en_match:
                    continue
                level_en = level_en_match.group(1).strip()
                
                # Extract behaviors
                behaviors = ""
                beh_match = re.search(r'\*\*Hành vi quan sát được\*\*:\n(.*?)\n\n', lblock, re.DOTALL)
                if beh_match:
                    behaviors = beh_match.group(1).strip()
                else:
                    # Try to match until the next ** or end
                    beh_match2 = re.search(r'\*\*Hành vi quan sát được\*\*:\n(.*?)(?=\n\*\*|$)', lblock, re.DOTALL)
                    if beh_match2:
                        behaviors = beh_match2.group(1).strip()
                
                data[group_en][level_en] = {
                    "definition": definition,
                    "behaviors": behaviors
                }
                
        return data

    def _parse_examples(self, path: str) -> Dict[str, Dict[str, str]]:
        # Returns mapping: group -> level -> example_text
        data = {}
        if not os.path.exists(path):
            return data
            
        with open(path, 'r', encoding='utf-8') as f:
            content = f.read()

        # Split by '## Tiêu chí' or '## [' 
        # Actually in examples-mcq it's '## Tiêu chí' then '### [C1-Basic-01] Mức Cơ bản'
        # In examples-situational it's '## [C1-Int-SIT-01] Tiêu chí 1: Nền tảng AI — Mức Trung cấp'
        
        # Let's use a simpler heuristic: find all blocks starting with `### [` or `## [`
        blocks = re.split(r'\n#{2,3} \[', content)
        for block in blocks[1:]:
            # The block starts with something like 'C1-Basic-01] Mức Cơ bản'
            header_end = block.find('\n')
            header = block[:header_end] if header_end != -1 else block
            body = block[header_end:].strip() if header_end != -1 else ""
            
            # Map header to group and level.
            # Example headers: 
            # "C1-Basic-01] Mức Cơ bản"
            # "C1-Int-SIT-01] Tiêu chí 1: Nền tảng AI — Mức Trung cấp"
            
            # We can extract level from Basic/Int/Adv
            level_en = "Basic"
            if "Int" in header or "Trung cấp" in header:
                level_en = "Intermediate"
            elif "Adv" in header or "Nâng cao" in header:
                level_en = "Advanced"
                
            # Extract criterion mapping
            group_en = ""
            if "C1" in header: group_en = "Fundamental"
            elif "C2" in header: group_en = "Data"
            elif "C3" in header: group_en = "Critical Thinking"
            elif "C4" in header: group_en = "AI Use Cases"
            elif "C5" in header: group_en = "AI Ethics"
            elif "C6" in header: group_en = "AI Tools"
            elif "C7" in header: group_en = "Future of Work"
            
            if group_en and level_en:
                if group_en not in data:
                    data[group_en] = {}
                if level_en not in data[group_en]:
                    data[group_en][level_en] = []
                data[group_en][level_en].append(body)
                
        return data

    def get_context(self, group: str, level: str, is_scenario: bool) -> dict:
        group_data = self.framework_data.get(group, {}).get(level, {})
        definition = group_data.get("definition", f"Khái niệm về {group}")
        behaviors = group_data.get("behaviors", "- Nhận diện các khái niệm cơ bản.")
        
        # Get examples
        examples_dict = self.sit_examples if is_scenario else self.mcq_examples
        examples_list = examples_dict.get(group, {}).get(level, [])
        if not examples_list:
            # Fallback to any level if specific level not found
            for lvl, ex_list in examples_dict.get(group, {}).items():
                if ex_list:
                    examples_list = ex_list
                    break
                    
        examples_text = "\n\n---\n\n".join(examples_list[:2]) if examples_list else "(Không có ví dụ mẫu)"
        
        return {
            "definition": definition,
            "behaviors": behaviors,
            "examples": examples_text
        }

# For testing
if __name__ == "__main__":
    parser = FrameworkParser(
        "../../src/question-generator/skills/references/framework.md",
        "../../src/question-generator/skills/references/examples-mcq.md",
        "../../src/question-generator/skills/references/examples-situational.md"
    )
    print(parser.get_context("Fundamental", "Basic", False))
