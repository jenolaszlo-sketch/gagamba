#!/usr/bin/env bash
# Run strict Linux qualification as the checkout owner inside a fresh,
# root-created and then delegated cgroup. Invoke through sudo on ubuntu-26.04.
set -Eeuo pipefail

if [[ "${1:-}" == --child ]]; then
  test "$(id -u)" != 0
  grep -Fq "/$(basename "$GAGAMBA_LINUX_CGROUP")" /proc/self/cgroup
  cd "$GAGAMBA_WORKSPACE"
  cleanup_build_servers() {
    status=$?
    dotnet build-server shutdown || status=1
    exit "$status"
  }
  trap cleanup_build_servers EXIT
  dotnet test tests/Gagamba.Conformance.Tests/Gagamba.Conformance.Tests.csproj -c Release --nologo
  version="$(python3 -c 'import xml.etree.ElementTree as E; print(E.parse("Directory.Build.props").findtext(".//Version"))')"
  python3 eng/verify_package_consumer.py --packages-dir candidate \
    --version "$version" --source-sha "$GAGAMBA_SOURCE_SHA" \
    --output qualification/consumer-linux.json
  exit 0
fi

test "$#" = 5
test "$(id -u)" = 0
test "$(stat -fc %T /sys/fs/cgroup)" = cgroup2fs
test -f /sys/fs/cgroup/cgroup.controllers

workspace="$1"
source_sha="$2"
run_id="$3"
attempt="$4"
report="$5"
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]]
[[ "$run_id" =~ ^[0-9]+$ ]]
[[ "$attempt" =~ ^[0-9]+$ ]]
test -f "$workspace/Gagamba.sln"

uid="$(stat -c %u "$workspace")"
gid="$(stat -c %g "$workspace")"
test "$uid" != 0
runner_home="${QUALIFICATION_HOME_OVERRIDE:-$(getent passwd "$uid" | cut -d: -f6)}"
test -d "$runner_home"
test "$(stat -c %u "$runner_home")" = "$uid"

parent="/sys/fs/cgroup/gagamba-qualification-${run_id}-${attempt}"
temp_dir="/tmp/gagamba-qualification-${run_id}-${attempt}"
parent_created=false
temp_created=false
finish() {
  status=$?
  if [[ "$parent_created" == true ]]; then
    for _ in $(seq 1 50); do
      if grep -q '^populated 0$' "$parent/cgroup.events"; then break; fi
      sleep 0.2
    done
    cat "$parent/cgroup.events"
    if [[ -n "$(find "$parent" -mindepth 1 -maxdepth 1 -type d -print)" ]] \
      || ! grep -q '^populated 0$' "$parent/cgroup.events"; then
      echo 'delegated qualification cgroup not empty' >&2
      status=1
    else
      rmdir "$parent" || status=1
    fi
  fi
  if [[ "$temp_created" == true && "$(realpath -m "$temp_dir")" == "/tmp/gagamba-qualification-${run_id}-${attempt}" ]]; then
    rm -rf -- "$temp_dir" || status=1
  elif [[ "$temp_created" == true ]]; then
    echo 'refusing unexpected qualification temp path' >&2
    status=1
  fi
  exit "$status"
}
trap finish EXIT

mkdir "$parent"
parent_created=true
mkdir "$temp_dir"
temp_created=true
chown "$uid:$gid" "$temp_dir"
chmod 700 "$temp_dir"
test -f "$parent/cgroup.kill"
chown "$uid:$gid" "$parent" "$parent/cgroup.procs" "$parent/cgroup.subtree_control"
export GAGAMBA_LINUX_CGROUP="$parent"
export GAGAMBA_WORKSPACE="$workspace"
export GAGAMBA_SOURCE_SHA="$source_sha"
export GAGAMBA_QUALIFICATION=1
export GAGAMBA_QUALIFICATION_OUTPUT="$report"
export QUALIFICATION_TMPDIR="$temp_dir"
export QUALIFICATION_UID="$uid" QUALIFICATION_GID="$gid" QUALIFICATION_HOME="$runner_home"
export QUALIFICATION_SCRIPT="$workspace/eng/qualify-linux-delegated.sh"

bash -c '
  echo $$ > "$GAGAMBA_LINUX_CGROUP/cgroup.procs"
  exec setpriv --reuid="$QUALIFICATION_UID" --regid="$QUALIFICATION_GID" --clear-groups \
    env HOME="$QUALIFICATION_HOME" DOTNET_CLI_HOME="$QUALIFICATION_HOME" \
    TMPDIR="$QUALIFICATION_TMPDIR" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    bash "$QUALIFICATION_SCRIPT" --child
'
