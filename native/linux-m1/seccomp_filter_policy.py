#!/usr/bin/env python3
"""Generate the pinned x86-64 classic seccomp BPF policy for WP04."""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
import struct
import sys


GENERATOR_VERSION = "4"
AUDIT_ARCH_X86_64 = 0xC000003E
X32_SYSCALL_BIT = 0x40000000

BPF_LD_W_ABS = 0x0020
BPF_JMP_JEQ_K = 0x0015
BPF_JMP_JGE_K = 0x0035
BPF_JMP_JSET_K = 0x0045
BPF_RET_K = 0x0006

SECCOMP_RET_KILL_THREAD = 0x00000000
SECCOMP_RET_ERRNO = 0x00050000
SECCOMP_RET_ALLOW = 0x7FFF0000
EPERM = 1
ENOSYS = 38

SYS_ARCH_PRCTL = 158
ARCH_SET_FS = 0x1002
SYS_PRCTL = 157
PR_SET_DUMPABLE = 4
PR_GET_DUMPABLE = 3
SYS_CLOSE_RANGE = 436
CLOSE_RANGE_FIRST = 6
CLOSE_RANGE_LAST = 0xFFFFFFFF
CLOSE_RANGE_FLAGS = 0
SYS_CLONE = 56
SYS_CLONE3 = 435
CLONE_NAMESPACE_MASK = 0x7E020080

EXPLICIT_DENIALS = (
    ("socket", 41),
    ("vhangup", 53),
    ("ptrace", 101),
    ("pivot_root", 155),
    ("chroot", 161),
    ("mount", 165),
    ("umount2", 166),
    ("unshare", 272),
    ("setns", 308),
    ("process_vm_readv", 310),
    ("process_vm_writev", 311),
    ("pidfd_send_signal", 424),
    ("pidfd_open", 434),
    ("pidfd_getfd", 438),
    ("open_tree", 428),
    ("move_mount", 429),
    ("fsopen", 430),
    ("fsconfig", 431),
    ("fsmount", 432),
    ("mount_setattr", 442),
)


@dataclass(frozen=True)
class Instruction:
    code: int
    jt: int = 0
    jf: int = 0
    k: int = 0
    note: str = ""


def build_instructions() -> tuple[Instruction, ...]:
    """Return the auditable BPF instruction sequence in kernel byte order."""
    p = [
        Instruction(BPF_LD_W_ABS, k=4, note="load seccomp_data.arch"),
        Instruction(BPF_JMP_JEQ_K, jt=1, k=AUDIT_ARCH_X86_64, note="require AUDIT_ARCH_X86_64"),
        Instruction(BPF_RET_K, k=SECCOMP_RET_KILL_THREAD, note="reject another audit architecture"),
        Instruction(BPF_LD_W_ABS, k=0, note="load seccomp_data.nr"),
        Instruction(BPF_JMP_JGE_K, jf=1, k=X32_SYSCALL_BIT, note="test x32 syscall-number bit"),
        Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | EPERM, note="reject x32 ABI"),
    ]

    for name, number in EXPLICIT_DENIALS:
        p.append(Instruction(BPF_JMP_JEQ_K, jf=1, k=number, note=f"match forbidden syscall {name} ({number})"))
        p.append(Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | EPERM, note=f"deny {name} with EPERM"))

    p.extend(
        (
            Instruction(BPF_JMP_JEQ_K, jf=1, k=SYS_CLONE3, note="match clone3"),
            Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | ENOSYS, note="make clone3 unavailable for Bubblewrap fallback"),
        )
    )

    # The old clone tail's non-clone path tests the syscall number against the
    # namespace mask. Preserve that existing disposition for every syscall
    # except narrowly constrained arch_prctl and prctl startup operations.
    arch_prctl_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=SYS_ARCH_PRCTL, note="match arch_prctl for one constrained startup operation"))
    p.append(Instruction(BPF_LD_W_ABS, k=16, note="load arch_prctl code (seccomp_data.args[0])"))
    arch_code_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=ARCH_SET_FS, note="allow ARCH_SET_FS only"))
    p.append(Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | EPERM, note="deny other arch_prctl operations with EPERM"))

    prctl_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=SYS_PRCTL, note="match prctl for one constrained hardening operation"))
    prctl_option_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=16, note="load prctl option (seccomp_data.args[0] low word)"))
    prctl_option_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=PR_SET_DUMPABLE, note="allow only PR_SET_DUMPABLE"))
    prctl_option_high_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=20, note="require zero high word for prctl option"))
    prctl_option_high_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="require exact PR_SET_DUMPABLE option value"))
    prctl_value_low_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=24, note="load prctl value (seccomp_data.args[1] low word)"))
    prctl_value_low_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="require dumpable disabled"))
    prctl_value_high_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=28, note="require zero high word for prctl value"))
    prctl_value_high_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="allow exact PR_SET_DUMPABLE value zero"))
    prctl_get_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=PR_GET_DUMPABLE, note="allow read-only PR_GET_DUMPABLE query"))
    prctl_get_high_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=20, note="require zero high word for prctl query option"))
    prctl_get_high_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="require exact PR_GET_DUMPABLE option value"))
    prctl_deny_pc = len(p)
    p.append(Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | EPERM, note="deny other prctl operations/arguments with EPERM"))

    close_range_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=SYS_CLOSE_RANGE, note="match close_range for exact coordinator descriptor closure"))
    close_range_first_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=16, note="load close_range first descriptor low word"))
    close_range_first_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=CLOSE_RANGE_FIRST, note="require first descriptor 6"))
    close_range_first_high_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=20, note="load close_range first descriptor high word"))
    close_range_first_high_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="require exact first descriptor"))
    close_range_last_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=24, note="load close_range last descriptor low word"))
    close_range_last_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=CLOSE_RANGE_LAST, note="require UINT_MAX last descriptor"))
    close_range_last_high_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=28, note="load close_range last descriptor high word"))
    close_range_last_high_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="require exact last descriptor"))
    close_range_flags_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=32, note="load close_range flags low word"))
    close_range_flags_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=CLOSE_RANGE_FLAGS, note="require flags zero"))
    close_range_flags_high_pc = len(p)
    p.append(Instruction(BPF_LD_W_ABS, k=36, note="load close_range flags high word"))
    close_range_flags_high_match_pc = len(p)
    p.append(Instruction(BPF_JMP_JEQ_K, k=0, note="require exact flags"))
    close_range_deny_pc = len(p)
    p.append(Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | EPERM, note="deny all other close_range arguments with EPERM"))
    clone_pc = len(p)
    p.extend(
        (
            Instruction(BPF_JMP_JEQ_K, jf=1, k=SYS_CLONE, note="match clone; preserve old non-clone path"),
            Instruction(BPF_LD_W_ABS, k=16, note="load clone flags for clone only"),
            Instruction(BPF_JMP_JSET_K, jf=1, k=CLONE_NAMESPACE_MASK, note="reject namespace-creating clone flags"),
            Instruction(BPF_RET_K, k=SECCOMP_RET_ERRNO | EPERM, note="deny clone namespace flags with EPERM"),
        )
    )
    allow_pc = len(p)
    p.append(Instruction(BPF_RET_K, k=SECCOMP_RET_ALLOW, note="preserved default ALLOW disposition"))

    # Non-arch_prctl syscalls proceed to the constrained prctl check. The exact
    # ARCH_SET_FS, PR_SET_DUMPABLE(0), and the exact close_range descriptor closure
    # jump to the preserved final ALLOW.
    p[arch_prctl_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=prctl_pc - arch_prctl_pc - 1,
        k=SYS_ARCH_PRCTL,
        note="match arch_prctl for one constrained startup operation",
    )
    p[close_range_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=clone_pc - close_range_pc - 1,
        k=SYS_CLOSE_RANGE,
        note="match close_range for exact coordinator descriptor closure",
    )
    p[close_range_first_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=close_range_deny_pc - close_range_first_match_pc - 1,
        k=CLOSE_RANGE_FIRST,
        note="require first descriptor 6",
    )
    p[close_range_first_high_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=close_range_deny_pc - close_range_first_high_match_pc - 1,
        k=0,
        note="require exact first descriptor",
    )
    p[close_range_last_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=close_range_deny_pc - close_range_last_match_pc - 1,
        k=CLOSE_RANGE_LAST,
        note="require UINT_MAX last descriptor",
    )
    p[close_range_last_high_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=close_range_deny_pc - close_range_last_high_match_pc - 1,
        k=0,
        note="require exact last descriptor",
    )
    p[close_range_flags_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=close_range_deny_pc - close_range_flags_match_pc - 1,
        k=CLOSE_RANGE_FLAGS,
        note="require flags zero",
    )
    p[close_range_flags_high_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jt=allow_pc - close_range_flags_high_match_pc - 1,
        k=0,
        note="require exact flags",
    )
    p[prctl_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=close_range_pc - prctl_pc - 1,
        k=SYS_PRCTL,
        note="match prctl for one constrained hardening operation",
    )
    p[prctl_option_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=prctl_get_pc - prctl_option_match_pc - 1,
        k=PR_SET_DUMPABLE,
        note="allow only PR_SET_DUMPABLE",
    )
    p[prctl_option_high_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=prctl_deny_pc - prctl_option_high_match_pc - 1,
        k=0,
        note="require exact PR_SET_DUMPABLE option value",
    )
    p[prctl_value_low_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=prctl_deny_pc - prctl_value_low_match_pc - 1,
        k=0,
        note="require dumpable disabled",
    )
    p[prctl_get_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jf=prctl_deny_pc - prctl_get_pc - 1,
        k=PR_GET_DUMPABLE,
        note="allow read-only PR_GET_DUMPABLE query",
    )
    p[prctl_get_high_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jt=allow_pc - prctl_get_high_match_pc - 1,
        k=0,
        note="require exact PR_GET_DUMPABLE option value",
    )
    p[prctl_value_high_match_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jt=allow_pc - prctl_value_high_match_pc - 1,
        k=0,
        note="allow exact PR_SET_DUMPABLE value zero",
    )
    p[arch_code_pc] = Instruction(
        BPF_JMP_JEQ_K,
        jt=allow_pc - arch_code_pc - 1,
        k=ARCH_SET_FS,
        note="allow ARCH_SET_FS only",
    )
    return tuple(p)


def build_filter() -> bytes:
    return b"".join(struct.pack("<HBBI", ins.code, ins.jt, ins.jf, ins.k) for ins in build_instructions())


def format_hex(data: bytes) -> str:
    return data.hex() + "\n"


def disassemble() -> str:
    rows = []
    for index, ins in enumerate(build_instructions()):
        rows.append(
            f"{index:02d}: code=0x{ins.code:04x} jt={ins.jt} jf={ins.jf} "
            f"k=0x{ins.k:08x} ({ins.k})  {ins.note}"
        )
    return "\n".join(rows) + "\n"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--write", action="store_true", help="regenerate seccomp-filter.hex")
    modes.add_argument("--check", action="store_true", help="verify seccomp-filter.hex matches this source")
    modes.add_argument("--binary", action="store_true", help="write raw BPF bytes to stdout")
    modes.add_argument("--disassemble", action="store_true", help="print the generated instruction listing")
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("seccomp-filter.hex"))
    parser.add_argument("--version", action="version", version=f"seccomp_filter_policy.py {GENERATOR_VERSION} (Python standard library; x86-64 classic BPF)")
    args = parser.parse_args(argv)
    data = build_filter()

    if args.binary:
        sys.stdout.buffer.write(data)
        return 0
    if args.disassemble:
        sys.stdout.write(disassemble())
        return 0
    if args.write:
        args.output.write_text(format_hex(data), encoding="ascii", newline="\n")
        print(f"wrote {len(data)} bytes to {args.output}")
        return 0

    try:
        checked_in = args.output.read_text(encoding="ascii")
    except OSError as exc:
        print(f"cannot read {args.output}: {exc}", file=sys.stderr)
        return 1
    if checked_in != format_hex(data):
        print(f"{args.output} does not match generated filter; run --write", file=sys.stderr)
        return 1
    print(f"{args.output}: generated x86-64 filter matches ({len(data)} bytes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
