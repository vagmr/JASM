import subprocess
import sys
import re

def get_latest_tag():
    result = subprocess.run(['git', 'describe', '--tags', '--abbrev=0'], capture_output=True, text=True)
    return result.stdout.strip()

def parse_commit(commit):
    pattern = r'^(\w+)(\([\w\s-]+\))?!?: (.+)$'
    match = re.match(pattern, commit)
    if match:
        type, scope, description = match.groups()
        scope = scope[1:-1] if scope else None
        return type, scope, description
    return None, None, commit

def generate_changelog(current_tag):
    previous_tag = get_latest_tag()
    
    result = subprocess.run(['git', 'log', f'{previous_tag}..{current_tag}', '--pretty=format:%s'], capture_output=True, text=True)
    commits = result.stdout.strip().split('\n')
    
    changelog = f"## 变更日志 (v{current_tag})\n\n"
    
    changes = {
        'feat': [],
        'fix': [],
        'perf': [],
        'refactor': [],
        'other': []
    }
    
    for commit in commits:
        type, scope, description = parse_commit(commit)
        if type in changes:
            changes[type].append(f"{description} {f'({scope})' if scope else ''}")
        else:
            changes['other'].append(description)
    
    for type, messages in changes.items():
        if messages:
            if type == 'feat':
                changelog += "### 新功能\n\n"
            elif type == 'fix':
                changelog += "### 修复\n\n"
            elif type == 'perf':
                changelog += "### 性能优化\n\n"
            elif type == 'refactor':
                changelog += "### 代码重构\n\n"
            elif type == 'other':
                changelog += "### 其他更改\n\n"
            
            for message in messages:
                changelog += f"- {message}\n"
            changelog += "\n"
    
    result = subprocess.run(['git', 'log', f'{previous_tag}..{current_tag}', '--pretty=format:%an'], capture_output=True, text=True)
    contributors = set(result.stdout.strip().split('\n'))
    
    changelog += f"## 贡献者\n\n"
    for contributor in contributors:
        changelog += f"- {contributor}\n"
    
    return changelog

if __name__ == "__main__":
    current_tag = sys.argv[1] if len(sys.argv) > 1 else "HEAD"
    changelog = generate_changelog(current_tag)
    print(changelog)
