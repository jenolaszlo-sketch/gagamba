#!/usr/bin/env bash
set -euo pipefail
umask 022

usage() {
  printf 'Usage: %s <canonical-gagamba-repo> <empty-output-directory>\n' "$0" >&2
}

if [[ $# -ne 2 ]]; then usage; exit 2; fi
repo=$(realpath "$1")
out=$(realpath -m "$2")
git_safe() { git -c "safe.directory=$repo" -C "$repo" "$@"; }
if [[ $(id -u) -eq 0 ]]; then
  echo 'refusing to build as root' >&2
  exit 1
fi
if [[ ! -d "$repo/.git" || ! -f "$repo/Gagamba.sln" ]]; then
  echo 'repo must be a Gagamba Git checkout' >&2
  exit 1
fi
branch=$(git_safe symbolic-ref --quiet --short HEAD || true)
if [[ "$branch" != main ]]; then
  echo 'refusing: canonical source must be on main' >&2
  exit 1
fi
if [[ -e "$out" && ( ! -d "$out" || -n "$(find "$out" -mindepth 1 -maxdepth 1 -print -quit)" ) ]]; then
  echo 'output path must be absent or an empty directory' >&2
  exit 1
fi
mkdir -p "$out"

head=$(git_safe rev-parse HEAD)
tree=$(git_safe rev-parse "$head^{tree}")
stage=$(mktemp -d "${TMPDIR:-/tmp}/gagamba-candidate.XXXXXXXX")
cleanup() { rm -rf -- "$stage"; }
trap cleanup EXIT HUP INT TERM
mkdir "$stage/source" "$stage/publish" "$stage/candidate"
git_safe archive --format=tar "$head" src native eng Directory.Build.props Directory.Build.targets README.md | tar -xf - -C "$stage/source"
cmp -s "$0" "$stage/source/eng/linux-m1/build-install-candidate.sh" || { echo "builder differs from committed source" >&2; exit 1; }

dotnet publish "$stage/source/src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj" \
  -c Release -r linux-x64 --self-contained false \
  -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false \
  -p:RuntimeFrameworkVersion=10.0.14 -p:RollForward=Disable \
  -o "$stage/publish/owner"
dotnet publish "$stage/source/src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj" \
  -c Release -r linux-x64 --self-contained false \
  -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false \
  -p:RuntimeFrameworkVersion=10.0.14 -p:RollForward=Disable \
  -o "$stage/publish/recovery"
for component in owner recovery provider; do mkdir -p "$stage/candidate/$component"; done
mkdir -p "$stage/candidate/native"

copy_file() {
  local src=$1 dst=$2
  [[ -f "$src" && ! -L "$src" ]] || { echo "required regular file missing: $src" >&2; exit 1; }
  install -m 0444 -- "$src" "$dst"
}

for component in owner recovery; do
  entry="Gagamba.Execution.Linux.M1.${component^}"
  copy_file "$stage/publish/$component/$entry.dll" "$stage/candidate/$component/$entry.dll"
  copy_file "$stage/publish/$component/$entry.deps.json" "$stage/candidate/$component/$entry.deps.json"
  copy_file "$stage/publish/$component/$entry.runtimeconfig.json" "$stage/candidate/$component/$entry.runtimeconfig.json"
  copy_file "$stage/publish/$component/Gagamba.Execution.Linux.M1.dll" "$stage/candidate/$component/Gagamba.Execution.Linux.M1.dll"
  copy_file "$stage/publish/$component/Gagamba.Execution.dll" "$stage/candidate/$component/Gagamba.Execution.dll"
  python3 - "$stage/candidate/$component/$entry.deps.json" "$component" <<'PY'
import json, sys
path, component = sys.argv[1:]
with open(path, encoding='utf-8') as f:
    deps = json.load(f)
target = deps.get('runtimeTarget', {}).get('name', '')
if target != '.NETCoreApp,Version=v10.0/linux-x64':
    raise SystemExit(f'{component} deps target is not the exact .NET 10 linux-x64 target: {target!r}')
entries = deps.get('targets', {}).get(target, {})
prefixes = {
    'owner': ('Gagamba.Execution.Linux.M1.Owner/', 'Gagamba.Execution.Linux.M1/', 'Gagamba.Execution/'),
    'recovery': ('Gagamba.Execution.Linux.M1.Recovery/', 'Gagamba.Execution.Linux.M1/', 'Gagamba.Execution/'),
}[component]
if any(not name.startswith(prefixes) for name in entries):
    raise SystemExit(f'{component} deps has an unexpected runtime library: {sorted(entries)}')
runtime_assets = set()
for name, entry in entries.items():
    if entry.get('runtimeTargets') or entry.get('native'):
        raise SystemExit(f'{component} deps includes RID fallback/native assets: {name}')
    for asset in entry.get('runtime', {}):
        runtime_assets.add(asset.rsplit('/', 1)[-1])
expected = {
    'Gagamba.Execution.Linux.M1.Owner.dll' if component == 'owner' else 'Gagamba.Execution.Linux.M1.Recovery.dll',
    'Gagamba.Execution.Linux.M1.dll', 'Gagamba.Execution.dll',
}
if runtime_assets != expected:
    raise SystemExit(f'{component} exact runtime asset set differs: expected={sorted(expected)}, actual={sorted(runtime_assets)}')
PY
  python3 - "$stage/candidate/$component/$entry.runtimeconfig.json" "$component" <<'PY'
import json, sys
path, component = sys.argv[1:]
with open(path, encoding='utf-8') as f:
    cfg = json.load(f)
opts = cfg.get('runtimeOptions', {})
framework = opts.get('framework', {})
if framework != {'name': 'Microsoft.NETCore.App', 'version': '10.0.14'}:
    raise SystemExit(f'{component} runtime framework is not exact: {framework!r}')
if opts.get('rollForward') != 'Disable':
    raise SystemExit(f'{component} rollForward is not Disable: {opts.get("rollForward")!r}')
if opts.get('additionalProbingPaths') or opts.get('includedFrameworks'):
    raise SystemExit(f'{component} runtimeconfig contains probing or additional frameworks')
PY
done

cmp -s "$stage/candidate/owner/Gagamba.Execution.Linux.M1.dll" "$stage/candidate/recovery/Gagamba.Execution.Linux.M1.dll" \
  || { echo 'Owner and Recovery published different M1 bytes; refusing closure substitution' >&2; exit 1; }
cmp -s "$stage/candidate/owner/Gagamba.Execution.dll" "$stage/candidate/recovery/Gagamba.Execution.dll" \
  || { echo 'Owner and Recovery published different contract bytes; refusing closure substitution' >&2; exit 1; }
copy_file "$stage/candidate/owner/Gagamba.Execution.Linux.M1.dll" "$stage/candidate/provider/Gagamba.Execution.Linux.M1.dll"
copy_file "$stage/candidate/owner/Gagamba.Execution.dll" "$stage/candidate/provider/Gagamba.Execution.dll"

gcc -static -O2 -s -fno-ident -frandom-seed=gagamba-wp04 \
  -Wl,--build-id=none -I"$stage/source/native/linux-m1" \
  -o "$stage/candidate/native/exec-coordinator" \
  "$stage/source/native/linux-m1/exec-coordinator.c"
chmod 0555 "$stage/candidate/native/exec-coordinator"
python3 "$stage/source/native/linux-m1/seccomp_filter_policy.py" --check
python3 "$stage/source/native/linux-m1/seccomp_filter_policy.py" --binary > "$stage/candidate/native/seccomp-filter.bin"
chmod 0444 "$stage/candidate/native/seccomp-filter.bin"

python3 "$stage/source/eng/linux-m1/build-install-candidate.py" \
  --repo "$repo" --source-commit "$head" --source-tree "$tree" \
  --staged-source "$stage/source" --candidate "$stage/candidate" --output "$out"

printf 'candidate: %s\n' "$out"
cat "$out/artifact-set.sha256"
