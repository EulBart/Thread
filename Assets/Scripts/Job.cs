using System;
using System.Threading;
using UnityEngine;

public abstract class Job<T> where T : struct 
{
    public delegate void ExecuteDelegate (ref T e);

    private Container<T> container;

    protected Job(Container<T> source)
    {
        container = source.readOnly ? new Container<T>(source) : source;
    }

    private System.Object threadLock = new object();
    private Thread[] threads;

    protected abstract ExecuteDelegate callback { get;}

    public delegate void LogFunction(string s);

    public LogFunction Log = null;



    private void Execute(int threadCount)
    {
        lock (threadLock)
        {
            int current = 0;
            int end = container.Count;
            int batchCount = end/threadCount;
            threads = new Thread[threadCount];
            if(Log != null)
                Log("Starting " + threadCount + " threads");
            for(int startedThreads = 0; startedThreads < threadCount;++startedThreads)
            {
                int last = current + batchCount;
                if(last > end)
                    last = end;
                int first = current;
                if(Log!=null)
                    Log("t"+startedThreads+ " will compute from " + first + " to " + last);
                threads[startedThreads] = new Thread(() => container.Execute(callback, first, last));
                threads[startedThreads].Start();
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

    public WaitYield Run(int threadCount)
    {
        Execute(threadCount);
        return new WaitYield(this);
    }
}