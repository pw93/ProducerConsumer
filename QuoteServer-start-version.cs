using System;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using MasterQuoteCS;
using MasterQuoteCS_DAPI;
using ProfitWin.Logging;


namespace ProfitWin
{
    public class QuoteServer : IDisposable
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _commandTask;
        private Task _publishTask;

        private bool _publishing = false;
        private readonly object _lock = new object();
        private bool _started = false;

        PublisherSocket _publisher_socket;
        private bool _disposed = false;

        public QuoteServer()
        {
            _publisher_socket = new PublisherSocket("@tcp://*:5556");
            
            

        }

        public void Dispose()
        {
            Stop();
            WaitUntilExit();
            
            Dispose(true);
            //GC.SuppressFinalize(this); // Optional: prevent finalizer from running
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                System.Console.WriteLine("ERROR dispose more than once");
                return;
            }

            if (disposing)
            {
                // Dispose managed resources
                _cts?.Dispose();
                _publisher_socket?.Dispose();
                //NetMQConfig.Cleanup();

            }

            // No unmanaged resources to clean up here

            _disposed = true;
        }
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
            //_publisher_socket = new PublisherSocket("@tcp://*:5556");
            //using (_publisher_socket = new PublisherSocket("@tcp://*:5556"))
            using (var repSocket = new ResponseSocket("@tcp://*:5555"))
            {


                Console.WriteLine("Server started.");
                Console.WriteLine("Listening for commands on tcp://*:5555");
                Console.WriteLine("Publishing on tcp://*:5556");


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
                                NetMQConfig.Cleanup();
                                return;

                            default:
                                repSocket.SendFrame("Unknown command.");
                                break;
                        }
                    }
                }
            }
        }

        void QuoteApi_OnLoginResultEvent_DAPI(bool aIsSucc, string aMsg)//登入是否成功
        {
            string sframe = $"[@@ufserver][OnLoginResultEvent_DAPI] aIsSucc:{aIsSucc}, aMsg:{aMsg}";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnAnnouncementEvent_DAPI(string data)//公告
        {

            string sframe = $"[@@ufserver][OnAnnouncementEvent_DAPI] data:{data}";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnVerifiedEvent_DAPI(TVerifyResult data) //驗證結果
        {
            string sframe = $"[@@ufserver][OnVerifiedEvent_DAPI] data.IsSucc:{data.IsSucc}, "
                + $"data.Msg:{data.Msg}, data.MarketKind: {data.MarketKind}";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnSystemEvent_DAPI(SystemEvent data)//訊息
        {
            string sframe = $"[@@ufserver][OnSystemEvent_DAPI] data:({data.ToString()})";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnUpdateBasic_DAPI(ProductBasic data)//傳來驗證商品資料
        {
            string sframe = $"[@@ufserver][OnUpdateBasic_DAPI] data:({data.ToString()})";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnMatch_DAPI(ProductTick data)//傳來驗證成交行情
        {
            string sframe = $"[@@ufserver][OnMatch_DAPI] data:({data.ToString()})";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }

        /// <summary>系統訊息通知</summary>
        private void Observer_OnSystemEvent(SystemEvent data)
        {
            string sframe = $"[@@ufserver][OnSystemEvent] data:({data.ToString()})";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
        }
        TaskCompletionSource<bool> tcs_connect;
        int connect_state_update = 0;
        bool is_connnected = false;
        /// <summary>系統連線狀態</summary>
        private void Observer_OnConnectState(bool aIsConnected, string aMsg)
        {
            string sframe = $"[@@ufserver][OnConnectState] aIsConnected:{aIsConnected}, aMsg:{aMsg}";
            _publisher_socket.SendFrame(sframe);
            Logger.logi(sframe);
            if (!tcs_connect.Task.IsCompleted)
                tcs_connect.SetResult(aIsConnected);
        }

        /// <summary>(國內)商品最新快照更新</summary>
        private void Observer_OnUpdateLastSnapshot(ProductSnapshot snapshot)
        {
            Logger.logi($"OnSnapshot: {snapshot.BasicData.Symbol}");
            try
            {
                string symbol = snapshot.BasicData.Symbol;
                var tick = snapshot.TickData;

                if (symbol == "TXFH5" && tick != null) //@@
                {
                    string ref_price = tick.RefPrice;
                    string price = tick.MatchPrice;
                    string qty = tick.MatchQty;
                    string matchTime = tick.MatchTime;
                    string timestamp = DateTime.UtcNow.Ticks.ToString(); // or .ToString("o") for ISO 8601
                    string message = $"{timestamp}#{symbol}#{matchTime}#{price}#{ref_price}";
                    Logger.logi(message);

                    //pub.Send(symbol, message);

                    //string sframe = $"[@@ufserver][OnConnectState] aIsConnected:{aIsConnected}, aMsg:{aMsg}";
                    _publisher_socket.SendFrame(message);


                    Logger.logi($"TXFH5 Quote - Price: {price}, Qty: {qty}");
                }
            }
            catch (Exception ex)
            {
                Logger.logi($"Error in OnSnapshot: {ex.Message}");
            }
        }
        private async Task PublishLoop(CancellationToken token)
        {
            

            var rand = new Random();
            tcs_connect = new TaskCompletionSource<bool> ();

            var quoteEvent = new MarketDataMart();
            var quoteApi = new MasterQuoteDAPI(quoteEvent);
            quoteApi.OnLoginResultEvent_DAPI += QuoteApi_OnLoginResultEvent_DAPI;//登入是否成功            
            quoteApi.OnAnnouncementEvent_DAPI += QuoteApi_OnAnnouncementEvent_DAPI;//公告
            quoteApi.OnVerifiedEvent_DAPI += QuoteApi_OnVerifiedEvent_DAPI;//驗證結果
            quoteApi.OnSystemEvent_DAPI += QuoteApi_OnSystemEvent_DAPI;//驗證失敗訊息
            quoteApi.OnUpdateBasic_DAPI += QuoteApi_OnUpdateBasic_DAPI;//傳來驗證商品資料
            quoteApi.OnMatch_DAPI += QuoteApi_OnMatch_DAPI;//傳來驗證成交行情

            quoteEvent.OnSystemEvent += Observer_OnSystemEvent;//系統訊息通知
            quoteEvent.OnConnectState += Observer_OnConnectState;//系統連線狀態
            quoteEvent.OnUpdateLastSnapshot += Observer_OnUpdateLastSnapshot;//國內商品最新快照更新

            quoteEvent.CreateParam.LoadProdBas_Fut = true;


            //==========================
            Logger.logi("Connecting to quote server...");
            quoteApi.Login("J121898429", "w19780903", true); // Sim = true //@@
            Logger.logi("Subscribe  Subscribe100");
            var is_connected = await tcs_connect.Task;

            //await Task.Delay(10000);
            Logger.logi("Subscribe  Subscribe200");
            quoteApi.Subscribe(EXCHANGE.TWF, "TXFH5");
            Logger.logi("Subscribe  Subscribe");
            if (is_connected)
            {
                while (!token.IsCancellationRequested && !is_connected)
                {


                    //string price = $"StockXYZ {DateTime.Now:HH:mm:ss} {rand.Next(100, 200)}";
                    //_publisher_socket.SendFrame(price);
                    //Console.WriteLine("Published: " + price);

                    await Task.Delay(1000, token); // 1 second interval
                }
            }
            else
            {
                Logger.loge("can't connect");
                string sframe = $"[@@ufserver][PublishLoop] connect fail";
                _publisher_socket.SendFrame(sframe);
            }
            quoteApi.Disconnect();


        }

        
    }
}

