using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Runtime;
using Gagamba.Execution.Windows;
using Gagamba.Execution.Linux;
using Gagamba.Execution.MacOS;

// Construct only BCL collections. Never construct providers or activate ExecutionRuntime.
Console.WriteLine(string.Join(",", new List<ExecutionRuntime>().Count,
    new List<WindowsExecutionProvider>().Count, new List<LinuxExecutionProvider>().Count,
    new List<MacOsExecutionProvider>().Count, new List<Penghou.Hufu.AuthorityAction>().Count));
if (!RestrictedManifestCodec.Supports(1, "linux-m1-native-transaction")) throw new Exception("API resolution");
Console.WriteLine("PASS: coherent Runtime/provider compile and managed type loading; Hufu core restored; no native activation or Hufu Sandbox integration.");
