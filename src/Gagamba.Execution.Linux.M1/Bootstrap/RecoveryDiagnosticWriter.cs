using System.Collections.Concurrent;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Diagnostic files cannot delay a safety reader, reaping, or helper termination.
// One background writer and one pending item; a stalled disk cannot grow a queue.
internal sealed class RecoveryDiagnosticWriter : IDisposable
{
    private readonly BlockingCollection<(string Name,object Value)> _pending=new(1);
    private readonly string _directory;
    internal RecoveryDiagnosticWriter(string directory)
    {
        _directory=directory;
        new Thread(()=>
        {
            foreach(var item in _pending.GetConsumingEnumerable())
                try{RecoveryEvidence.Write(_directory,item.Name,item.Value);}catch(IOException){}catch(UnauthorizedAccessException){}
        }){IsBackground=true,Name="gagamba-recovery-diagnostic"}.Start();
    }
    internal void Post(string name,object value) {if(!_pending.IsAddingCompleted) _pending.TryAdd((name,value));}
    public void Dispose()=>_pending.CompleteAdding(); // no join: diagnostics never own process lifetime
}
