using System;
using System.Threading;
using UnityEngine;

public abstract class Job<T> where T : struct 
{
    public delegate void ExecuteDelegate (int index, ref T e);

    private Container<T> container;

    protected Job(Container<T> source)
    {
        container = source.readOnly ? new Container<T>(source) : source;
    }

    private System.Object threadLock = new object();
    private Thread[] threads;

    protected abstract ExecuteDelegate callback { get;}

    public delegate void LogFunction(string s);

    /*
    public readonly LogFunction Log = Debug.Log;
    /*/
    public readonly LogFunction Log = null;
    //*/

    private void Execute(int threadCount)
    {
        if(threadCount<0)
            threadCount=1;
        lock (threadLock)
        {
            int current = 0;
            int end = container.Count;
            if(end == 0)
                return;
            if(threadCount > end)
                threadCount = end;
            int batchCount = end / threadCount;
            threads = new Thread[threadCount];
            if (Log != null)
                Log(GetType().Name + " Starting " + threadCount + " threads for " + end + " " + typeof(T).Name);
            for (int startedThreads = 0; startedThreads < threadCount; ++startedThreads)
            {
                int last = current + batchCount;
                if (last > end)
                    last = end;
                int first = current;
                if (Log != null)
                    Log("t" + startedThreads + " will compute from " + first + " to " + last);
                Thread nT = new Thread(() => container.Execute(callback, first, last));

                threads[startedThreads] = nT;
                nT.Start();
                current += batchCount;
            }
        }
    }

    public class WaitYield : CustomYieldInstruction
    {
        private Job<T> job;
        public WaitYield(Job<T> job)
        {
            this.job=job;
        }

        public T[] result
        {
            get
            {
                if(job.KeepWaiting)
                    throw new Exception("Accessing result of unfinished job");
                return job.container.Array;
            }
        }

        public override bool keepWaiting
        {
            get
            {
                return job.KeepWaiting;
            }
        }
    }

    private bool KeepWaiting
    {
        get
        {
            for (int index = threads.Length; --index>=0;)
            {
                if (threads[index].IsAlive) return true;
            }
            return false;
        }
    }

    public void Stop()
    {
        foreach (Thread thread in threads)
        {
            thread.Abort();
        }
    }

    public T[] RunSync(int threadCount)
    {
        Execute(threadCount);
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        return container.Array;
    }

    public WaitYield RunAsync(int threadCount)
    {
        Execute(threadCount);
        return new WaitYield(this);
    }


}