import subprocess
import os
import sys
import json
import shutil

unity_exe = r'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe'
project_root = r'C:\vsc\restavrator'
shadow_root = r'C:\vsc\test_shadow'

def is_project_locked(proj):
    lockfile = os.path.join(proj, 'Temp', 'UnityLockfile')
    if not os.path.exists(lockfile):
        return False
    try:
        with open(lockfile, 'r+b'):
            return False
    except IOError:
        return True

def ensure_shadow_project(source, shadow):
    os.makedirs(shadow, exist_ok=True)
    junctions = ['Assets', 'Packages', 'ProjectSettings', 'Library']
    for j in junctions:
        src_dir = os.path.join(source, j)
        tgt_dir = os.path.join(shadow, j)
        if not os.path.exists(tgt_dir) and os.path.exists(src_dir):
            subprocess.run(f'mklink /J "{tgt_dir}" "{src_dir}"', shell=True, check=True)

target_project = project_root
if is_project_locked(project_root):
    print("Detected active Unity Editor process holding project lock. Using safe shadow workspace...")
    ensure_shadow_project(project_root, shadow_root)
    target_project = shadow_root
else:
    print("Unity Editor is not holding lock. Running tests on primary project...")

log_file = os.path.join(target_project, 'test_run.log')
results_json_target = os.path.join(target_project, 'Build', 'test_results.json')
results_json_dest = os.path.join(project_root, 'Build', 'test_results.json')

if os.path.exists(results_json_target):
    os.remove(results_json_target)
if os.path.exists(results_json_dest):
    os.remove(results_json_dest)

cmd = [unity_exe, '-batchmode', '-quit', '-projectPath', target_project, '-executeMethod', 'E2ETestRunner.RunAllTests', '-logFile', log_file]
print(f'Starting Unity E2ETestRunner on {target_project}...')
res = subprocess.run(cmd, capture_output=True, text=True, timeout=360)
print('Process exit code:', res.returncode)

if target_project != project_root:
    if os.path.exists(results_json_target):
        os.makedirs(os.path.join(project_root, 'Build'), exist_ok=True)
        shutil.copy2(results_json_target, results_json_dest)

chosen_json = results_json_dest if os.path.exists(results_json_dest) else (results_json_target if os.path.exists(results_json_target) else None)

if chosen_json and os.path.exists(chosen_json):
    with open(chosen_json, 'r', encoding='utf-8-sig') as f:
        data = json.load(f)
    total = data.get('totalTests', 0)
    passed = data.get('passed', data.get('passedTests', 0))
    failed = data.get('failed', data.get('failedTests', 0))
    duration = data.get('durationSeconds', 0)
    print(f"Results: Total={total}, Passed={passed}, Failed={failed} (took {duration}s)")
    if failed > 0:
        for item in data.get('results', []):
            if not item.get('passed'):
                print(f"FAILURE: {item.get('tier')}.{item.get('name')} - {item.get('errorMessage')}")
        sys.exit(1)
    else:
        print(f"SUCCESS: All {passed}/{total} tests passed 100%!")
        sys.exit(0)
else:
    print('test_results.json not found! Check log tail:')
    if os.path.exists(log_file):
        with open(log_file, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()
            for l in lines[-40:]:
                print(l.rstrip())
    sys.exit(res.returncode if res.returncode != 0 else 1)
