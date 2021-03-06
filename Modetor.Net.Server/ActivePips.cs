﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Modetor.Net.Server
{
    class ActivePips
    {
        

        public ActivePips(int numberOfThreads)
        {
            ThreadsCount = numberOfThreads;
            Threads = new Thread[numberOfThreads];
            Lockers = new ManualResetEventSlim[numberOfThreads];
            Queues = new Queue<Action>[numberOfThreads];
            Busy = new bool[numberOfThreads];
            for (int i = numberOfThreads-1; i > -1; i--)
            {
                Busy[i] = false;
                Queues[i] = new Queue<Action>();
                Lockers[i] = new ManualResetEventSlim(false);
                Threads[i] = new Thread(Consume) { IsBackground = true, Name = i.ToString(), Priority = ThreadPriority.BelowNormal };
                Threads[i].Start();
            }
        }
        public ActivePips() : this(DefaultNumberOfThread) { }

        private void Consume()
        {
            int ID = Convert.ToInt32(Thread.CurrentThread.Name);
            while (KeepAlive)
            {
                while (Queues[ID].Count == 0 && KeepAlive) { /*if(Configuration.DEBUG_MODE) Console.WriteLine("I [ActivePips] : Thread[{0}] - Moving to wait mode", ID);*/ Lockers[ID].Wait(); }

                Busy[ID] = true;
                
                for (int i = 0; i < Queues[ID].Count; i++)
                {
                    if (KeepAlive)
                        Queues[ID].Dequeue().Invoke();
                    else break;
                }
                Lockers[ID].Reset();
                Busy[ID] = false;
                /*if (Configuration.DEBUG_MODE) Console.WriteLine("I [ActivePips] : Thread[{0}] - No Processes left", ID);*/
            }
        }

        public void AddWork(Action act)
        {
            bool signaled = false;
            for (int i = 0; i < ThreadsCount; i++)
            {
                if (!Busy[i] && Queues[i].Count == 0)
                {
                    /*if (Configuration.DEBUG_MODE) Console.WriteLine("I [ActivePips] : Thread[{0}] - +1 Process in queue ", i);*/
                    Queues[i].Enqueue(act);
                    Lockers[i].Set();
                    
                    signaled = true;
                    break;
                }
            }
            
            if(!signaled)
            {
                int rnd = new Random().Next(0, ThreadsCount - 1);
                /* if (Configuration.DEBUG_MODE) Console.WriteLine("I [ActivePips] : Thread[{0}] - +1 Process in queue (randomly picked)", rnd);*/
                Queues[rnd].Enqueue(act);
                Lockers[rnd].Set();
                
                
            }
        }
        [Obsolete("Warning: This method puts the work on random threads. This method isn't deprecated!",false)]
        public void AddWorkToThread(uint ID, Action act)
        {
            if(ID >= ThreadsCount) { throw new IndexOutOfRangeException($"Thread ID ({ID}) out of range ({ThreadsCount} - 1)"); }
            Queues[ID].Enqueue(act);
            Lockers[ID].Set();
        }

        public void Suspend() {
            Suspended = true;
            foreach (ManualResetEventSlim locker in Lockers)
                locker.Wait();
        }
        public void Resume()
        {
            Suspended = false;
            foreach (ManualResetEventSlim locker in Lockers)
                locker.Wait();
        }
        public bool IsSuspended() => Suspended;
        public void ClearWorks()
        {
            foreach (Queue<Action> q in Queues)
                q.Clear();
        }
        public void Kill()
        {
            KeepAlive = true;
            foreach (Queue<Action> q in Queues)
                q.Clear();
        }
        private Thread[] Threads;
        private Queue<Action>[] Queues;
        private bool[] Busy;
        private ManualResetEventSlim[] Lockers;
        private readonly int ThreadsCount;
        private bool Suspended = false;
        public bool KeepAlive { get; private set; } = true;

        public static readonly int DefaultNumberOfThread = Environment.ProcessorCount;

    }
}
