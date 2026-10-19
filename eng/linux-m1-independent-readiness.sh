#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_REV="38f6981b2a60daab2df37c92913a6760c1bef131"
EXPECTED_FILTER_SHA256="2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108"
EXPECTED_FILTER_SIZE=672
EXPECTED_BWRAP_SHA256="523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6"
EXPECTED_BWRAP_VERSION="bubblewrap 0.11.1"
DELEGATED_PARENT="${GAGAMBA_DELEGATED_CGROUP:-/sys/fs/cgroup/gaga-delegated}"
REPORT=""
WORK_DIR=""
PROBE_CGROUP=""
PASS=0
FAIL=0
declare -a FAILED_CHECKS=()

usage() {
  cat <<'EOF'
WP05 independent Linux qualification environment preflight.

Usage:
  bash eng/linux-m1-independent-readiness.sh [options]

Options:
  --source-rev SHA        Exact source revision to verify (default: R12)
  --delegated-parent DIR  Operator-delegated cgroup v2 parent
                          (default: /sys/fs/cgroup/gaga-delegated)
  --report PATH           Also write the preflight output to PATH
  -h, --help              Show this help

This checks the Linux host, unprivileged delegated cgroup, toolchain, pinned
Bubblewrap, frozen seccomp filter, exact source availability, and writable
workspace. It does not build, run tests, execute a target, or claim independent
reviewer readiness.
EOF
}

ok() { PASS=$((PASS + 1)); printf 'PASS  %s: %s\n' "$1" "$2"; }
bad() { FAIL=$((FAIL + 1)); FAILED_CHECKS+=("$1"); printf 'FAIL  %s: %s\n' "$1" "$2"; }
info() { printf 'INFO  %s: %s\n' "$1" "$2"; }
have() { command -v "$1" >/dev/null 2>&1; }
has_token() { tr ' ' '\n' <"$1" 2>/dev/null | grep -qx "$2"; }

cleanup() {
  local status=$?
  if [[ -n "$PROBE_CGROUP" && -d "$PROBE_CGROUP" ]]; then
    rmdir -- "$PROBE_CGROUP" 2>/dev/null || {
      printf 'FAIL  cleanup.probe_cgroup: could not remove %s\n' "$PROBE_CGROUP"
      status=1
    }
  fi
  if [[ -n "$WORK_DIR" && -d "$WORK_DIR" ]]; then
    rm -rf -- "$WORK_DIR" || {
      printf 'FAIL  cleanup.workspace: could not remove %s\n' "$WORK_DIR"
      status=1
    }
  fi
  if [[ "$FAIL" -gt 0 && "$status" -eq 0 ]]; then
    status=1
  fi
  if [[ "$FAIL" -gt 0 ]]; then
    printf 'FAILED_CHECKS=%s\n' "${FAILED_CHECKS[*]}"
  fi
  printf 'SUMMARY pass=%d fail=%d\n' "$PASS" "$FAIL"
  printf 'INDEPENDENT_REVIEWER_ACCESS=UNVERIFIED\n'
  printf 'INDEPENDENT_ACCEPTANCE=NOT_CLAIMED\n'
  exit "$status"
}
while [[ $# -gt 0 ]]; do
  case "$1" in
    --source-rev)
      [[ $# -ge 2 ]] || { echo "missing value for --source-rev" >&2; exit 2; }
      SOURCE_REV="$2"
      shift 2
      ;;
    --delegated-parent)
      [[ $# -ge 2 ]] || { echo "missing value for --delegated-parent" >&2; exit 2; }
      DELEGATED_PARENT="$2"
      shift 2
      ;;
    --report)
      [[ $# -ge 2 ]] || { echo "missing value for --report" >&2; exit 2; }
      REPORT="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

[[ "$SOURCE_REV" =~ ^[0-9a-f]{40}$ ]] || { echo "source-rev must be a 40-character SHA-1" >&2; exit 2; }
[[ "$DELEGATED_PARENT" = /* ]] || { echo "delegated-parent must be absolute" >&2; exit 2; }
DELEGATED_PARENT="$(realpath -m -- "$DELEGATED_PARENT")"
[[ "$DELEGATED_PARENT" == /sys/fs/cgroup/* ]] || {
  echo "delegated-parent must be beneath /sys/fs/cgroup" >&2
  exit 2
}

if [[ -z "$REPORT" ]]; then
  REPORT=""
fi

if [[ -n "$REPORT" ]]; then
  report_dir="$(dirname -- "$REPORT")"
  [[ -d "$report_dir" ]] || { echo "report directory does not exist: $report_dir" >&2; exit 2; }
  : >"$REPORT" || { echo "report path is not writable: $REPORT" >&2; exit 2; }
  exec > >(tee -a -- "$REPORT") 2>&1
fi
trap cleanup EXIT

for tool in bash uname id stat getent git sha256sum wc file python3 gcc dotnet bwrap mktemp awk grep tr realpath dirname tee; do
  if have "$tool"; then
    ok "tool.$tool" "$(command -v "$tool")"
  else
    bad "tool.$tool" "missing from PATH"
  fi
done
if have uname; then
  info "host.uname" "$(uname -srm)"
  [[ "$(uname -s)" == Linux ]] && ok "host.linux" "Linux" || bad "host.linux" "$(uname -s)"
  [[ "$(uname -m)" == x86_64 ]] && ok "host.x86_64" "x86_64" || bad "host.x86_64" "$(uname -m)"
fi
if [[ -f /etc/os-release ]]; then
  info "host.distro" "$(awk -F= '/^PRETTY_NAME=/{gsub(/"/,"",$2); print $2}' /etc/os-release)"
fi
if have id; then
  uid="$(id -u)"
  gid="$(id -g)"
  info "identity" "uid=$uid gid=$gid groups=$(id -G)"
  [[ "$uid" != 0 ]] && ok "identity.nonroot" "uid=$uid" || bad "identity.nonroot" "must not be root"
fi

if [[ -r /proc/self/status ]]; then
  for cap in CapInh CapPrm CapEff CapAmb; do
    value="$(awk -v key="$cap:" '$1 == key { print $2 }' /proc/self/status)"
    if [[ "$value" =~ ^0+$ ]]; then
      ok "capability.$cap" "$value"
    else
      bad "capability.$cap" "${value:-missing} (must be zero)"
    fi
  done
  info "capability.CapBnd" "$(awk '$1 == "CapBnd:" { print $2 }' /proc/self/status)"
  info "NoNewPrivs" "$(awk '$1 == "NoNewPrivs:" { print $2 }' /proc/self/status)"
else
  bad "capability.status" "/proc/self/status unreadable"
fi

if [[ "$(stat -f -c %T /sys/fs/cgroup 2>/dev/null || true)" == cgroup2fs ]]; then
  ok "cgroup.unified" "cgroup2fs"
else
  bad "cgroup.unified" "cgroup2fs not mounted at /sys/fs/cgroup"
fi
if [[ -f /sys/fs/cgroup/cgroup.controllers ]]; then
  ok "cgroup.controllers" "$(cat /sys/fs/cgroup/cgroup.controllers)"
else
  bad "cgroup.controllers" "/sys/fs/cgroup/cgroup.controllers missing"
fi

current_cgroup_raw="$(awk -F: '$1 == "0" { print $3 }' /proc/self/cgroup 2>/dev/null || true)"
current_cgroup=""
if [[ -n "$current_cgroup_raw" ]]; then
  if [[ "$current_cgroup_raw" == /sys/fs/cgroup/* ]]; then
    current_cgroup="$(realpath -m -- "$current_cgroup_raw")"
  elif [[ -d "$DELEGATED_PARENT$current_cgroup_raw" ]]; then
    current_cgroup="$(realpath -m -- "$DELEGATED_PARENT$current_cgroup_raw")"
  else
    current_cgroup="$(realpath -m -- "/sys/fs/cgroup$current_cgroup_raw")"
  fi
fi
info "cgroup.current_raw" "${current_cgroup_raw:-unknown}"
info "cgroup.current" "${current_cgroup:-unknown}"
if [[ -n "$current_cgroup" && "$current_cgroup" == "$DELEGATED_PARENT"/* ]]; then
  ok "cgroup.placement" "$current_cgroup is beneath $DELEGATED_PARENT"
else
  bad "cgroup.placement" "${current_cgroup:-unknown} is not beneath $DELEGATED_PARENT"
fi

if [[ -d "$DELEGATED_PARENT" ]]; then
  ok "delegated.exists" "$DELEGATED_PARENT"
  if [[ -r "$DELEGATED_PARENT/cgroup.controllers" ]]; then
    controllers="$(cat "$DELEGATED_PARENT/cgroup.controllers")"
    info "delegated.controllers" "$controllers"
    for controller in memory pids; do
      has_token "$DELEGATED_PARENT/cgroup.controllers" "$controller" \
        && ok "delegated.controller.$controller" "offered" \
        || bad "delegated.controller.$controller" "not offered"
    done
  else
    bad "delegated.controllers" "$DELEGATED_PARENT/cgroup.controllers unreadable"
  fi
  if [[ -r "$DELEGATED_PARENT/cgroup.subtree_control" ]]; then
    subtree="$(cat "$DELEGATED_PARENT/cgroup.subtree_control")"
    info "delegated.subtree_control" "$subtree"
    for controller in memory pids; do
      has_token "$DELEGATED_PARENT/cgroup.subtree_control" "$controller" \
        && ok "delegated.subtree.$controller" "delegated" \
        || bad "delegated.subtree.$controller" "not delegated"
    done
  else
    bad "delegated.subtree_control" "$DELEGATED_PARENT/cgroup.subtree_control unreadable"
  fi
  [[ -f "$DELEGATED_PARENT/cgroup.kill" ]] \
    && ok "delegated.cgroup_kill" "cgroup.kill present" \
    || bad "delegated.cgroup_kill" "cgroup.kill missing"
  [[ -w "$DELEGATED_PARENT" ]] \
    && ok "delegated.writable" "$DELEGATED_PARENT writable" \
    || bad "delegated.writable" "$DELEGATED_PARENT not writable"
else
  bad "delegated.exists" "$DELEGATED_PARENT missing"
fi

if [[ -n "$current_cgroup" && -d "$current_cgroup" ]]; then
  if [[ -w "$current_cgroup" && -w "$current_cgroup/cgroup.procs" ]]; then
    ok "delegated.placement.writable" "$current_cgroup and cgroup.procs writable"
  else
    bad "delegated.placement.writable" "$current_cgroup is not writable"
  fi
  if [[ -w "$DELEGATED_PARENT" ]]; then
    PROBE_CGROUP="$DELEGATED_PARENT/gagamba-readiness-$$"
    if mkdir -- "$PROBE_CGROUP" 2>/dev/null; then
      ok "delegated.create" "$PROBE_CGROUP"
      if [[ -f "$PROBE_CGROUP/memory.max" && -f "$PROBE_CGROUP/pids.max" ]]; then
        ok "delegated.control_files" "memory.max and pids.max present"
        if printf 'max\n' >"$PROBE_CGROUP/memory.max" 2>/dev/null \
          && printf 'max\n' >"$PROBE_CGROUP/pids.max" 2>/dev/null; then
          ok "delegated.control_write" "memory.max and pids.max accept max"
        else
          bad "delegated.control_write" "could not write delegated controller files"
        fi
      else
        bad "delegated.control_files" "memory.max or pids.max missing"
      fi
      rmdir -- "$PROBE_CGROUP" 2>/dev/null \
        && ok "delegated.remove" "$PROBE_CGROUP removed" \
        || bad "delegated.remove" "could not remove $PROBE_CGROUP"
      PROBE_CGROUP=""
    else
      bad "delegated.create" "could not create $PROBE_CGROUP"
      PROBE_CGROUP=""
    fi
  fi
else
  bad "delegated.placement" "current cgroup is unavailable"
fi

if have dotnet; then
  dotnet_version="$(dotnet --version 2>/dev/null || true)"
  info "toolchain.dotnet" "$dotnet_version"
  if [[ "$dotnet_version" =~ ^10\. ]]; then
    ok "toolchain.dotnet.major" "10.x"
  else
    bad "toolchain.dotnet.major" "$dotnet_version is not a .NET 10 SDK"
  fi
fi
if have gcc; then
  info "toolchain.gcc" "$(gcc --version 2>/dev/null | head -1)"
fi
if have python3; then
  info "toolchain.python" "$(python3 --version 2>&1)"
fi
if have ldd; then
  info "toolchain.libc" "$(ldd --version 2>/dev/null | head -1)"
fi

if have bwrap; then
  bwrap_path="$(command -v bwrap)"
  bwrap_version="$(bwrap --version 2>&1 || true)"
  bwrap_sha="$(sha256sum -- "$bwrap_path" | awk '{ print $1 }')"
  bwrap_mode="$(stat -c '%a' -- "$bwrap_path")"
  info "bwrap.path" "$bwrap_path"
  info "bwrap.version" "$bwrap_version"
  info "bwrap.mode" "$bwrap_mode"
  [[ "$bwrap_version" == "$EXPECTED_BWRAP_VERSION" ]] \
    && ok "bwrap.version.pinned" "$EXPECTED_BWRAP_VERSION" \
    || bad "bwrap.version.pinned" "$bwrap_version"
  [[ "$bwrap_sha" == "$EXPECTED_BWRAP_SHA256" ]] \
    && ok "bwrap.sha256" "$bwrap_sha" \
    || bad "bwrap.sha256" "$bwrap_sha"
  if (( (8#$bwrap_mode & 04000) == 0 )); then
    ok "bwrap.nonsetuid" "mode $bwrap_mode"
  else
    bad "bwrap.nonsetuid" "mode $bwrap_mode has setuid"
  fi
fi

WORK_DIR="$(mktemp -d "${TMPDIR:-/tmp}/gagamba-wp05-readiness.XXXXXX" 2>/dev/null || true)"
if [[ -z "$WORK_DIR" || ! -d "$WORK_DIR" ]]; then
  bad "workspace.create" "mktemp failed"
  exit 1
fi
if printf 'readiness-probe\n' >"$WORK_DIR/probe" && [[ -s "$WORK_DIR/probe" ]]; then
  ok "workspace.write" "$WORK_DIR"
else
  bad "workspace.write" "$WORK_DIR is not writable"
fi
if printf 'report-probe\n' >"$WORK_DIR/readiness-report.txt" && [[ -s "$WORK_DIR/readiness-report.txt" ]]; then
  ok "report.write" "$WORK_DIR/readiness-report.txt"
else
  bad "report.write" "could not write report in $WORK_DIR"
fi

if have git; then
  if git -C "$ROOT" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    top="$(git -C "$ROOT" rev-parse --show-toplevel)"
    info "source.root" "$ROOT"
    info "source.top" "$top"
    info "source.head" "$(git -C "$ROOT" rev-parse HEAD)"
    info "source.rev" "$SOURCE_REV"
    if git -C "$ROOT" cat-file -e "$SOURCE_REV^{commit}" 2>/dev/null; then
      ok "source.revision" "$SOURCE_REV"
      if git -C "$ROOT" merge-base --is-ancestor "$SOURCE_REV" HEAD; then
        ok "source.ancestor" "$SOURCE_REV is an ancestor of HEAD"
      else
        bad "source.ancestor" "$SOURCE_REV is not an ancestor of HEAD"
      fi
      if git -C "$ROOT" diff --quiet "$SOURCE_REV" -- src native tests; then
        ok "source.production_surfaces" "src native tests match $SOURCE_REV"
      else
        bad "source.production_surfaces" "src native tests differ from $SOURCE_REV"
      fi
      if [[ -z "$(git -C "$ROOT" status --porcelain --untracked-files=no)" ]]; then
        ok "source.clean_tracked" "no tracked modifications"
      else
        bad "source.clean_tracked" "tracked modifications present"
      fi
    else
      bad "source.revision" "$SOURCE_REV is unavailable"
    fi
  else
    bad "source.repository" "$ROOT is not a Git work tree"
  fi
fi

filter_hex="$ROOT/native/linux-m1/seccomp-filter.hex"
filter_generator="$ROOT/native/linux-m1/seccomp_filter_policy.py"
if [[ -f "$filter_hex" && -f "$filter_generator" ]]; then
  ok "filter.source" "generator and checked-in hex present"
  if python3 "$filter_generator" --check >/dev/null 2>&1; then
    ok "filter.generator_check" "checked-in hex matches generator"
  else
    bad "filter.generator_check" "generator --check failed"
  fi
  filter_tmp="$WORK_DIR/seccomp-filter.bin"
  if python3 "$filter_generator" --binary >"$filter_tmp" 2>/dev/null; then
    filter_size="$(wc -c <"$filter_tmp")"
    filter_sha="$(sha256sum -- "$filter_tmp" | awk '{ print $1 }')"
    info "filter.generated" "size=$filter_size sha256=$filter_sha"
    [[ "$filter_size" -eq "$EXPECTED_FILTER_SIZE" ]] \
      && ok "filter.size" "$EXPECTED_FILTER_SIZE" \
      || bad "filter.size" "$filter_size"
    [[ "$filter_sha" == "$EXPECTED_FILTER_SHA256" ]] \
      && ok "filter.sha256" "$filter_sha" \
      || bad "filter.sha256" "$filter_sha"
  else
    bad "filter.generated" "generator --binary failed"
  fi
  artifact_filter="$ROOT/artifacts/linux-m1/wp04/seccomp-filter.bin"
  if [[ -f "$artifact_filter" ]]; then
    artifact_sha="$(sha256sum -- "$artifact_filter" | awk '{ print $1 }')"
    artifact_size="$(wc -c <"$artifact_filter")"
    info "filter.artifact" "size=$artifact_size sha256=$artifact_sha"
    [[ "$artifact_sha" == "$EXPECTED_FILTER_SHA256" && "$artifact_size" -eq "$EXPECTED_FILTER_SIZE" ]] \
      && ok "filter.artifact.pin" "matches frozen identity" \
      || bad "filter.artifact.pin" "artifact does not match frozen identity"
  else
    info "filter.artifact" "not present; generated pin is reproducible"
  fi
else
  bad "filter.source" "generator or checked-in hex missing"
fi

printf 'REVIEWER_ACCESS=UNVERIFIED\n'
printf 'READINESS=%s\n' "$([[ "$FAIL" -eq 0 ]] && echo PASS || echo FAIL)"
