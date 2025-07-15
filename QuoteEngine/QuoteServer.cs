using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProfitWin
{
    public class QuoteServer
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _commandTask;
        private Task _publishTask;

        private bool _publishing = false;
        private readonly object _lock = new object();
        private bool _started = false;



        public void Start()
        {
            lock (_lock)
            {
                if (_started) return;
                _started = true;
                _commandTask = Task.Run(() => CommandListener(_cts.Token));
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public void WaitUntilExit()
        {
            _commandTask?.Wait();
            _publishTask?.Wait();
        }

        private async Task CommandListener(CancellationToken token)
        {
            using (var repSocket = new ResponseSocket("@tcp://*:5555"))
            {


                Console.WriteLine("Server started.");
                Console.WriteLine("Listening for commands on tcp://*:5555");
                

                while (!token.IsCancellationRequested)
                {
                    if (repSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(200), out string command))
                    {
                        Console.WriteLine($"Received command: {command}");

                        switch (command)
                        {
                            case "start_pub":
                                lock (_lock)
                                {
                                    if (!_publishing)
                                    {
                                        _publishing = true;
                                        _publishTask = Task.Run(() => PublishLoop(_cts.Token));
                                    }
                                }
                                repSocket.SendFrame("Started publishing.");
                                break;

                            case "stop_pub":
                                lock (_lock)
                                {
                                    _publishing = false;
                                }
                                repSocket.SendFrame("Stopped publishing.");
                                break;

                            case "status":
                                string status;
                                lock (_lock)
                                {
                                    status = _publishing ? "Publishing" : "Idle";
                                }
                                repSocket.SendFrame($"Status: {status}");
                                break;

                            case "shutdown":
                                _publishing = false;
                                _cts.Cancel();
                                // Wait for publish task to end
                                if (_publishTask != null)
                                {
                                    try
                                    {
                                        var completed = await Task.WhenAny(_publishTask, Task.Delay(1000)); // 1 sec timeout
                                        if (completed != _publishTask)
                                            Console.WriteLine("Publish task did not complete in time.");
                                        else
                                            Console.WriteLine("Publish task completed.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Publish task error: " + ex.Message);
                                    }
                                }
                                repSocket.SendFrame("Server shutting down.");
                                await Task.Delay(200);
                                return;

                            default:
                                repSocket.SendFrame("Unknown command.");
                                break;
                        }
                    }
                }
            }
        }

        private async Task PublishLoop(CancellationToken token)
        {
            var rand = new Random();
            Console.WriteLine("Publishing on tcp://*:5556");
            using (var pubSocket = new PublisherSocket("@tcp://*:5556"))
            {


                while (!token.IsCancellationRequested)
                {


                    string price = $"StockXYZ {DateTime.Now:HH:mm:ss} {rand.Next(100, 200)}";
                    pubSocket.SendFrame(price);
                    Console.WriteLine("Published: " + price);

                    await Task.Delay(1000, token); // 1 second interval
                }
            }


        }
    }
}

