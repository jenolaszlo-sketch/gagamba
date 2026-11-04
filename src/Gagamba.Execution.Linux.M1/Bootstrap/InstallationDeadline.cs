namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class InstallationDeadline
{
    internal static long Now => OwnerSafetyMonitor.Now();
    internal static long Create(long? earlier=null) => Math.Min(checked(Now+7_000_000_000L),earlier??long.MaxValue);
    internal static void Validate(long deadline)
    {long left=deadline-Now;if(left<=0 || left>7_000_000_000L) throw new IOException("installation deadline refused");}
    internal static int Remaining(long deadline,int cap=5000)
    {long left=deadline-Now;if(left<=0) throw new IOException("installation deadline expired");return (int)Math.Clamp((left+999999)/1000000,1,cap);}
    // At most one verification worker in each process. A stuck worker blocks subsequent
    // attempts; helpers can exit without joining it and stop service never waits on hashing.
    private static int _worker;
    internal static T Verify<T>(Func<T> work,long deadline,Action service)
    {
        if(Interlocked.CompareExchange(ref _worker,1,0)!=0) throw new IOException("prior verifier not reclaimed");
        var job=Task.Run(()=>{try{return work();}finally{Volatile.Write(ref _worker,0);}});
        while(!job.IsCompleted) {service();_=Remaining(deadline);Thread.Sleep(2);}
        service();_=Remaining(deadline);return job.GetAwaiter().GetResult();
    }
}
