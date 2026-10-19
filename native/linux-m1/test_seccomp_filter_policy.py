"""Focused semantic regression tests for the pinned WP04 seccomp policy."""

import hashlib
from pathlib import Path
import unittest

from seccomp_filter_policy import (
    ARCH_SET_FS,
    AUDIT_ARCH_X86_64,
    CLOSE_RANGE_FIRST,
    CLOSE_RANGE_FLAGS,
    CLOSE_RANGE_LAST,
    BPF_JMP_JEQ_K,
    BPF_JMP_JGE_K,
    BPF_JMP_JSET_K,
    BPF_LD_W_ABS,
    BPF_RET_K,
    CLONE_NAMESPACE_MASK,
    ENOSYS,
    EPERM,
    EXPLICIT_DENIALS,
    PR_GET_DUMPABLE,
    PR_SET_DUMPABLE,
    SECCOMP_RET_ALLOW,
    SECCOMP_RET_ERRNO,
    SECCOMP_RET_KILL_THREAD,
    SYS_ARCH_PRCTL,
    SYS_PRCTL,
    SYS_CLOSE_RANGE,
    SYS_CLONE,
    SYS_CLONE3,
    X32_SYSCALL_BIT,
    build_filter,
    build_instructions,
    format_hex,
)


ROOT = Path(__file__).resolve().parent
FILTER_HEX = ROOT / "seccomp-filter.hex"
REPOSITORY = ROOT.parent.parent
FILTER_BIN = REPOSITORY / "artifacts/linux-m1/wp04/seccomp-filter.bin"
ADAPTER_SOURCE = REPOSITORY / "src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs"
BUILD_SCRIPT = REPOSITORY / "eng/linux-m1/build-native.sh"


def evaluate(program: bytes, number: int, arch: int = AUDIT_ARCH_X86_64, args: tuple[int, ...] = ()) -> int:
    accumulator = 0
    pc = 0
    while 0 <= pc < len(program) // 8:
        code = int.from_bytes(program[pc * 8 : pc * 8 + 2], "little")
        jt = program[pc * 8 + 2]
        jf = program[pc * 8 + 3]
        k = int.from_bytes(program[pc * 8 + 4 : pc * 8 + 8], "little")

        if code == BPF_LD_W_ABS:
            if k == 0:
                accumulator = number & 0xFFFFFFFF
            elif k == 4:
                accumulator = arch & 0xFFFFFFFF
            elif 16 <= k < 64 and (k - 16) % 4 == 0:
                arg_offset = k - 16
                arg_index = arg_offset // 8
                word_shift = 32 if arg_offset % 8 == 4 else 0
                argument = args[arg_index] if arg_index < len(args) else 0
                accumulator = (argument >> word_shift) & 0xFFFFFFFF
            else:
                raise AssertionError(f"unsupported seccomp_data offset {k}")
            pc += 1
        elif code in (BPF_JMP_JEQ_K, BPF_JMP_JGE_K, BPF_JMP_JSET_K):
            if code == BPF_JMP_JEQ_K:
                offset = jt if accumulator == k else jf
            elif code == BPF_JMP_JGE_K:
                offset = jt if accumulator >= k else jf
            else:
                offset = jt if accumulator & k else jf
            pc += 1 + offset
        elif code == BPF_RET_K:
            return k
        else:
            raise AssertionError(f"unsupported BPF opcode 0x{code:04x}")
    raise AssertionError("BPF program did not return a disposition")


def baseline_disposition(number: int, arch: int, args0: int = 0) -> int:
    """Model the checked-in 424-byte policy before the R2 rule was added.

    This historical model retains the clone-tail fallthrough defect: for
    non-clone syscalls it tests the syscall number against the namespace mask.
    The regression tests use it to prove the original defect is detected.
    """
    if arch != AUDIT_ARCH_X86_64:
        return SECCOMP_RET_KILL_THREAD
    if number >= X32_SYSCALL_BIT:
        return SECCOMP_RET_ERRNO | EPERM
    if number in {item[1] for item in EXPLICIT_DENIALS}:
        return SECCOMP_RET_ERRNO | EPERM
    if number == SYS_CLONE3:
        return SECCOMP_RET_ERRNO | ENOSYS
    if number == SYS_CLONE:
        return SECCOMP_RET_ERRNO | EPERM if args0 & CLONE_NAMESPACE_MASK else SECCOMP_RET_ALLOW
    # Preserve the old clone-tail non-clone JSET behavior exactly.
    if number & CLONE_NAMESPACE_MASK:
        return SECCOMP_RET_ERRNO | EPERM
    return SECCOMP_RET_ALLOW


class SeccompFilterPolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.program = FILTER_HEX.read_text(encoding="ascii").strip()
        cls.program = bytes.fromhex(cls.program)

    def test_checked_in_hex_is_deterministic_generator_output(self) -> None:
        self.assertEqual(FILTER_HEX.read_text(encoding="ascii"), format_hex(build_filter()))
        self.assertEqual(build_filter(), build_filter())
        self.assertEqual(len(build_instructions()), len(build_filter()) // 8)

    def test_arch_prctl_allows_only_arch_set_fs(self) -> None:
        for operation in (0x1001, 0x1003, 0x1004, 0x1011, 0x1012, 0xDEAD):
            with self.subTest(operation=hex(operation)):
                self.assertEqual(evaluate(self.program, SYS_ARCH_PRCTL, args=(operation, 0x7FFFFFFFFFFF)), SECCOMP_RET_ERRNO | EPERM)
        for fs_base in (0, 1, 0x1000, 0x7FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF):
            with self.subTest(fs_base=hex(fs_base)):
                self.assertEqual(evaluate(self.program, SYS_ARCH_PRCTL, args=(ARCH_SET_FS, fs_base)), SECCOMP_RET_ALLOW)

    def test_prctl_allows_only_dumpability_disable(self) -> None:
        denied_operations = (0, 1, 2, 5, 22, 36, 38, 0x59616D61)
        for operation in denied_operations:
            with self.subTest(operation=hex(operation)):
                self.assertEqual(
                    evaluate(self.program, SYS_PRCTL, args=(operation, 0)),
                    SECCOMP_RET_ERRNO | EPERM,
                )

        self.assertEqual(evaluate(self.program, SYS_PRCTL, args=(PR_GET_DUMPABLE, 0)), SECCOMP_RET_ALLOW)
        self.assertEqual(evaluate(self.program, SYS_PRCTL, args=(PR_GET_DUMPABLE, 0xDEADBEEF)), SECCOMP_RET_ALLOW)

        for option, value in (
            (PR_SET_DUMPABLE, 1),
            (PR_SET_DUMPABLE, 2),
            (PR_SET_DUMPABLE, 0x100000000),
            (0x100000004, 0),
            (0x100000003, 0),
        ):
            with self.subTest(option=hex(option), value=hex(value)):
                self.assertEqual(
                    evaluate(self.program, SYS_PRCTL, args=(option, value)),
                    SECCOMP_RET_ERRNO | EPERM,
                )

        self.assertEqual(
            evaluate(self.program, SYS_PRCTL, args=(PR_SET_DUMPABLE, 0)),
            SECCOMP_RET_ALLOW,
        )

    def test_close_range_allows_only_exact_descriptor_closure(self) -> None:
        allowed = (CLOSE_RANGE_FIRST, CLOSE_RANGE_LAST, CLOSE_RANGE_FLAGS)
        self.assertEqual(
            evaluate(self.program, SYS_CLOSE_RANGE, args=allowed),
            SECCOMP_RET_ALLOW,
        )

        denied = (
            (0, CLOSE_RANGE_LAST, CLOSE_RANGE_FLAGS),
            (12, CLOSE_RANGE_LAST, CLOSE_RANGE_FLAGS),
            (CLOSE_RANGE_FIRST, CLOSE_RANGE_LAST - 1, CLOSE_RANGE_FLAGS),
            (CLOSE_RANGE_FIRST, CLOSE_RANGE_LAST, 1),
            (CLOSE_RANGE_FIRST + (1 << 32), CLOSE_RANGE_LAST, CLOSE_RANGE_FLAGS),
            (CLOSE_RANGE_FIRST, CLOSE_RANGE_LAST + (1 << 32), CLOSE_RANGE_FLAGS),
            (CLOSE_RANGE_FIRST, CLOSE_RANGE_LAST, CLOSE_RANGE_FLAGS + (1 << 32)),
        )
        for arguments in denied:
            with self.subTest(arguments=tuple(hex(value) for value in arguments)):
                self.assertEqual(
                    evaluate(self.program, SYS_CLOSE_RANGE, args=arguments),
                    SECCOMP_RET_ERRNO | EPERM,
                )
    def test_clone_namespace_controls_and_existing_denials_remain(self) -> None:
        for bit in (0x80, 0x20000, 0x02000000, 0x04000000, 0x08000000, 0x10000000, 0x20000000, 0x40000000):
            with self.subTest(namespace_flag=hex(bit)):
                self.assertNotEqual(bit & CLONE_NAMESPACE_MASK, 0)
                self.assertEqual(evaluate(self.program, SYS_CLONE, args=(bit,)), SECCOMP_RET_ERRNO | EPERM)
        for flags in (0, 0x100, 0x400, 0x100 | 0x400):
            with self.subTest(clone_flags=hex(flags)):
                self.assertEqual(evaluate(self.program, SYS_CLONE, args=(flags,)), SECCOMP_RET_ALLOW)

        for name, number in EXPLICIT_DENIALS:
            with self.subTest(denied_syscall=name, number=number):
                self.assertEqual(evaluate(self.program, number), SECCOMP_RET_ERRNO | EPERM)
        self.assertEqual(evaluate(self.program, SYS_CLONE3), SECCOMP_RET_ERRNO | ENOSYS)

    def test_ordinary_syscalls_take_the_default_allow_path(self) -> None:
        constrained = {SYS_ARCH_PRCTL, SYS_PRCTL, SYS_CLOSE_RANGE, SYS_CLONE, SYS_CLONE3}
        denied = {item[1] for item in EXPLICIT_DENIALS}
        syscall_numbers = list(range(2048)) + [X32_SYSCALL_BIT - 1]
        for number in syscall_numbers:
            if number in constrained or number in denied:
                continue
            with self.subTest(number=number):
                self.assertEqual(evaluate(self.program, number), SECCOMP_RET_ALLOW)

    def test_clone_tail_jump_does_not_interpret_syscall_numbers_as_clone_flags(self) -> None:
        constrained = {SYS_ARCH_PRCTL, SYS_PRCTL, SYS_CLOSE_RANGE, SYS_CLONE, SYS_CLONE3}
        denied = {item[1] for item in EXPLICIT_DENIALS}
        affected = [
            number
            for number in range(1024)
            if number & CLONE_NAMESPACE_MASK and number not in constrained and number not in denied
        ]
        for number in (186, 202, 218, 231, 234):
            self.assertIn(number, affected)
        for number in affected:
            with self.subTest(number=number):
                self.assertEqual(
                    baseline_disposition(number, AUDIT_ARCH_X86_64),
                    SECCOMP_RET_ERRNO | EPERM,
                )
                self.assertEqual(evaluate(self.program, number), SECCOMP_RET_ALLOW)

    def test_generated_filter_matches_artifacts_and_production_pins(self) -> None:
        data = build_filter()
        digest = hashlib.sha256(data).hexdigest()
        self.assertEqual(FILTER_HEX.read_text(encoding="ascii"), format_hex(data))
        self.assertIn(f'FilterSha256 = "{digest}"', ADAPTER_SOURCE.read_text(encoding="utf-8"))
        self.assertIn(digest, BUILD_SCRIPT.read_text(encoding="utf-8"))
        if not FILTER_BIN.exists():
            self.skipTest("artifacts/linux-m1/wp04/seccomp-filter.bin not built; run eng/linux-m1/build-native.sh")
        self.assertEqual(FILTER_BIN.read_bytes(), data)

    def test_wrong_architecture_and_x32_remain_rejected(self) -> None:
        self.assertEqual(evaluate(self.program, 0, arch=0xC00000B7), SECCOMP_RET_KILL_THREAD)
        self.assertEqual(evaluate(self.program, X32_SYSCALL_BIT), SECCOMP_RET_ERRNO | EPERM)
        self.assertEqual(evaluate(self.program, X32_SYSCALL_BIT + 158, args=(ARCH_SET_FS,)), SECCOMP_RET_ERRNO | EPERM)


if __name__ == "__main__":
    unittest.main()
