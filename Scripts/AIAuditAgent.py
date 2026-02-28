import os
import sys
import json
import urllib.request
import urllib.error
from pathlib import Path

# =========================================================================================
# RecoveryCommander AI Security & Debugging Agent
# =========================================================================================
# This script acts as an autonomous AI agent to thoroughly inspect your application for 
# security vulnerabilities, bugs, and best practice violations.
#
# Requirements:
# - Python 3.x installed
# - OpenAI API Key in the environment variable `OPENAI_API_KEY`
#
# Usage:
# Run `python AIAuditAgent.py` in your terminal.
# The agent will scan all C# files, analyze them using an LLM, run build checks, 
# and output a comprehensive Markdown report.
# =========================================================================================

API_KEY = os.environ.get("OPENAI_API_KEY")
MODEL = "gpt-4o"  # You can change this to gpt-4-turbo, gpt-3.5-turbo, etc.
ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUTPUT_REPORT = os.path.join(ROOT_DIR, "Security_Audit_Report.md")

def call_llm(messages):
    """Sends a request to the OpenAI Chat Completions API."""
    if not API_KEY:
        print("[!] Missing OPENAI_API_KEY environment variable. Skipping LLM analysis.")
        return "SAFE (API Key Missing)"
        
    url = "https://api.openai.com/v1/chat/completions"
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {API_KEY}"
    }
    data = {
        "model": MODEL,
        "messages": messages,
        "temperature": 0.2
    }
    
    req = urllib.request.Request(url, data=json.dumps(data).encode("utf-8"), headers=headers)
    try:
        with urllib.request.urlopen(req) as response:
            result = json.loads(response.read().decode("utf-8"))
            return result["choices"][0]["message"]["content"]
    except urllib.error.HTTPError as e:
        error_msg = e.read().decode("utf-8")
        print(f"[!] API Error: {error_msg}")
        return f"Error communicating with AI: {error_msg}"
    except Exception as e:
        print(f"[!] Error: {str(e)}")
        return f"Error: {str(e)}"

def run_build_test():
    """Runs a dotnet build to check for compile-time errors."""
    print("[*] Agent: Running automated build verification...")
    result = os.popen(f"dotnet build \"{ROOT_DIR}\"").read()
    if "Build FAILED" in result or "error CS" in result:
        print("[!] Agent: Build failed. Errors detected!")
        return False, result
    print("[+] Agent: Build passed successfully.")
    return True, result

def audit_file(file_path):
    """Sends a source code file to the AI Agent for security and bug inspection."""
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    # Skip very small or empty files
    if len(content.strip()) < 20:
        return "SAFE"
        
    relative_path = os.path.relpath(file_path, ROOT_DIR)
    print(f"[*] Agent: Inspecting {relative_path}...")
    
    system_prompt = (
        "You are an expert autonomous AI Security Researcher and Senior C# Developer. "
        "Your task is to thoroughly analyze the provided C# code for a desktop utility application called RecoveryCommander. "
        "Look for:\n"
        "1. Security Vulnerabilities (Command Injection, Path Traversal, Arbitrary File Read/Write, DLL Hijacking, insecure HTTP, etc.)\n"
        "2. Concurrency/Async Bugs (Deadlocks, race conditions, unsafe thread access)\n"
        "3. Critical logical bugs and unhandled exceptions.\n\n"
        "If the file is completely safe and follows best practices, respond exactly with 'SAFE'. "
        "If you find issues, provide a structured Markdown response with the specific vulnerability, severity, and the exact code fix required."
    )
    
    messages = [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": f"File: {relative_path}\n\n```csharp\n{content}\n```"}
    ]
    
    return call_llm(messages)

def main():
    print("=====================================================")
    print("   RecoveryCommander AI Autonomous Security Agent    ")
    print("=====================================================")
    
    # Run Build first
    build_success, build_log = run_build_test()
    
    report_content = "# AI Autonomous Security & Debug Audit Report\n\n"
    report_content += "## 1. Automated Build Integrity\n"
    if build_success:
        report_content += "✅ **Result:** Build completed successfully with no critical compiler errors.\n\n"
    else:
        report_content += "❌ **Result:** Build Failed. The codebase currently has syntax or dependency errors.\n\n"
        report_content += f"```text\n{build_log[:1000]}...\n```\n\n"
    
    report_content += "## 2. In-Depth Security & Bug Inspection\n"
    
    cs_files = list(Path(ROOT_DIR).rglob("*.cs"))
    issues_found = 0
    
    for filepath in cs_files:
        # Avoid analyzing generated objective folders
        if "obj\\" in str(filepath) or "bin\\" in str(filepath):
            continue
            
        result = audit_file(str(filepath))
        
        if result.strip() != "SAFE" and not result.startswith("Error"):
            issues_found += 1
            rel_path = os.path.relpath(filepath, ROOT_DIR)
            report_content += f"### ⚠️ Issues found in `{rel_path}`\n\n"
            report_content += f"{result}\n\n"
            report_content += "---\n\n"
            
    if issues_found == 0:
        report_content += "\n🎉 **No high-risk vulnerabilities or bugs found! The codebase appears secure and well-structured.**\n"
        
    with open(OUTPUT_REPORT, "w", encoding="utf-8") as f:
        f.write(report_content)
        
    print(f"\n[+] Audit complete! Agent found {issues_found} files with issues.")
    print(f"[+] Full report saved to: {OUTPUT_REPORT}")

if __name__ == "__main__":
    main()
