using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualBasic;

/*
Example usage of separated IProducer<T> and IConsumer<T>:

var pc = new ProducerConsumer<int>();
var producer = pc.GetProducer();
var consumer = pc.GetConsumer();

Thread producerThread = new Thread(() =>
{
    for (int i = 0; i < 5; i++)
    {
        producer.Produce(i);
        Console.WriteLine($"Produced: {i}");
        Thread.Sleep(100);
    }
    producer.Complete();
    Console.WriteLine("Producer done");
});

Thread consumerThread = new Thread(() =>
{
    while (!consumer.IsDone())
    {
        var items = consumer.ConsumeAll();
        foreach (var item in items)
            Console.WriteLine($"Consumed: {item}");
    }
    Console.WriteLine("Consumer done");
});

producerThread.Start();
consumerThread.Start();
producerThread.Join();
consumerThread.Join();
*/

public interface IProducer<T>
{
    void Produce(T item);
    void Complete();
}

public interface IConsumer<T>
{
    List<T> ConsumeAll();
    bool IsDone();
}

public class ProducerConsumer<T>
{
    private readonly List<T> data = new List<T>();
    private readonly object lockObj = new object();
    private bool completed = false;
    private readonly AutoResetEvent dataAvailable = new AutoResetEvent(false);

    public IProducer<T> GetProducer() => new Producer(this);
    public IConsumer<T> GetConsumer() => new Consumer(this);

    private class Producer : IProducer<T>
    {
        private readonly ProducerConsumer<T> parent;
        public Producer(ProducerConsumer<T> parent) => this.parent = parent;

        public void Produce(T item)
        {
            lock (parent.lockObj)
            {
                if (parent.completed)
                    throw new InvalidOperationException("Cannot produce after completion.");
                parent.data.Add(item);
            }
            parent.dataAvailable.Set();
        }

        public void Complete()
        {
            lock (parent.lockObj)
            {
                parent.completed = true;
            }
            parent.dataAvailable.Set();
        }
    }

    private class Consumer : IConsumer<T>
    {
        private readonly ProducerConsumer<T> parent;
        public Consumer(ProducerConsumer<T> parent) => this.parent = parent;

        public List<T> ConsumeAll()
        {
            parent.dataAvailable.WaitOne();
            lock (parent.lockObj)
            {
                var batch = new List<T>(parent.data);
                parent.data.Clear();
                return batch;
            }
        }

        public bool IsDone()
        {
            lock (parent.lockObj)
            {
                return parent.completed && parent.data.Count == 0;
            }
        }
    }
}

class Program
{
    static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

        
    }
    static void LogInfo(string message,
    [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = System.IO.Path.GetFileName(filePath);
        string timestamp = DateTime.Now.ToString("MM/dd HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp} i][{fileName}:{lineNumber}] {message}");
    }

    static string label_log;
    static void log_text(string s,
     [CallerFilePath] string filePath = "",
     [CallerLineNumber] int lineNumber = 0)
    {
        label_log = s;
        LogInfo(s, filePath, lineNumber); // Pass along caller info
    }
    static void Main()
    {
        var pc = new ProducerConsumer<int>();
        var producer = pc.GetProducer();
        var consumer = pc.GetConsumer();

        Thread producerThread = new Thread(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                producer.Produce(i);
                LogInfo($"Produced: {i}");
                log_text($"Produced: {i}");
                Thread.Sleep(10);
            }
            producer.Complete();
            Log("Producer done");
        });

        Thread consumerThread = new Thread(() =>
        {
            while (!consumer.IsDone())
            {
                var items = consumer.ConsumeAll();
                foreach (var item in items)
                    Log($"Consumed: {item}");
                Thread.Sleep(100); // Simulate processing delay
            }
            Log("Consumer done");
        });

        producerThread.Start();
        consumerThread.Start();

        producerThread.Join();
        consumerThread.Join();

        Log("All done");
    }
}
