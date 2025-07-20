using System;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using MasterQuoteCS;
using MasterQuoteCS_DAPI;
using ProfitWin.Logging;
using IniParser.Model;
using ProfitWin.Common;
using IniParser;



namespace ProfitWin
{
    public class QuoteServer 
    {
        private CancellationTokenSource _cts;        
        private Task _publishTask;
        private Task _commandTask;
        PublisherSocket _publisher_socket_msg;
        PublisherSocket _publisher_socket_data;
        ResponseSocket _repSocket;
        //private NetMQPoller _poller;
        //private Task _pollerTask;
        //private Thread _pollerThread;

        string port_command;
        string port_message;
        string port_data;
        string user_id;
        string user_password;
        string product_symbol;
        string fname_ini = "ufserver.ini";

        private readonly object _lock = new object();

        private TaskCompletionSource<bool> _tcsStopped = null;

        public enum ServerState
        {
            StateIdle,
            StateStarting,
            StateRunning,
            StateClosingFromCommand,
            StateClosing,
        }
        private ServerState _state = ServerState.StateIdle;

        private readonly MarketDataMart _quoteEvent;
        private readonly MasterQuoteDAPI _quoteApi;

        public QuoteServer()
        {
            _quoteEvent = new MarketDataMart();
            _quoteApi = new MasterQuoteDAPI(_quoteEvent);
            _quoteApi.OnLoginResultEvent_DAPI += QuoteApi_OnLoginResultEvent_DAPI;//登入是否成功            
            _quoteApi.OnAnnouncementEvent_DAPI += QuoteApi_OnAnnouncementEvent_DAPI;//公告
            _quoteApi.OnVerifiedEvent_DAPI += QuoteApi_OnVerifiedEvent_DAPI;//驗證結果
            _quoteApi.OnSystemEvent_DAPI += QuoteApi_OnSystemEvent_DAPI;//驗證失敗訊息
            _quoteApi.OnUpdateBasic_DAPI += QuoteApi_OnUpdateBasic_DAPI;//傳來驗證商品資料
            _quoteApi.OnMatch_DAPI += QuoteApi_OnMatch_DAPI;//傳來驗證成交行情

            _quoteEvent.OnSystemEvent += Observer_OnSystemEvent;//系統訊息通知
            _quoteEvent.OnConnectState += Observer_OnConnectState;//系統連線狀態
            _quoteEvent.OnUpdateLastSnapshot += Observer_OnUpdateLastSnapshot;//國內商品最新快照更新

            _quoteEvent.CreateParam.LoadProdBas_Fut = true;
        }

        private async Task CommandListener(CancellationToken token)
        {
            





            while (!token.IsCancellationRequested)
            {

                if (_repSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(200), out string command))
                {
                    Logger.logi($"Received command: {command}");
                    lock (_lock)
                    {
                        if (_state == ServerState.StateIdle && command== "shutdown")
                        {
                            Logger.loge("server is idle. 'shutdown' is not allowed");
                            continue;
                        }
                        if (_state == ServerState.StateStarting && command == "shutdown")
                        {
                            Logger.loge("server is starting. 'shutdown' is not allowed");
                            continue;
                        }
                        if ((_state == ServerState.StateClosingFromCommand || _state == ServerState.StateClosing) && command == "shutdown")
                        {
                            Logger.loge("server is closing.");
                            continue;
                        }
                    }

                    switch (command)
                    {
                        case "shutdown":                            
                            _repSocket.SendFrame("Shutting down ...");
                            _ = Stop();
                            break;
                        case "status":
                            _repSocket.SendFrame($"Current state: {_state}");
                            break;

                        default:
                            _repSocket.SendFrame("Unknown command.");
                            break;
                    }                    
                }
                
            }

        }


        private void LoadConfig()
        {
            var ini_parser = new FileIniDataParser();
            IniData ini_data = ini_parser.ReadFile(fname_ini);
            
            port_command = ini_data["port"]["port_command"];
            port_message = ini_data["port"]["port_message"];
            port_data = ini_data["port"]["port_data"];            
            user_id = CryptoUtils.Decrypt(ini_data["user"]["id"], "ProfitWin");
            user_password = CryptoUtils.Decrypt(ini_data["user"]["password"], "ProfitWin");
            product_symbol = ini_data["product"]["symbol"];
        }
        

        public void Start()
        {
            lock (_lock)
            {
                if (_state != ServerState.StateIdle)
                {
                    Logger.loge("start only accept when server is idle.");
                    return;
                }
                _state = ServerState.StateStarting;
                _tcsStopped = new TaskCompletionSource<bool>();  // 重新建立新的 TCS
            }
            
            _cts = new CancellationTokenSource(); // 新增這行
            LoadConfig();
            _publisher_socket_msg = new PublisherSocket($"@tcp://*:{port_message}");
            _publisher_socket_data = new PublisherSocket($"@tcp://*:{port_data}");
            
            _repSocket = new ResponseSocket($"@tcp://*:{port_command}");
          

            _commandTask = Task.Run(() => CommandListener(_cts.Token));


            _publishTask = Task.Run(() => PublishLoop(_cts.Token));

            Logger.logi($"QuoteServer started on pub:{port_data}, cmd:{port_command}");
            lock (_lock)
            {
                _state = ServerState.StateRunning;
            }
        }
        
        private async Task Stop()
        {
            lock (_lock)
            {
                if (_state == ServerState.StateClosing || _state == ServerState.StateIdle)
                    return;
                if (_state == ServerState.StateClosingFromCommand)
                {
                    _state = ServerState.StateClosing;
                    return;
                }

                _state = ServerState.StateClosing;
            }
            Logger.logi("Stopping QuoteServer...");

            _cts.Cancel();     
            

            // Await the tasks asynchronously instead of blocking Wait()
            try
            {       
                if (_publishTask != null)
                {
                    try
                    {
                        var completedTask = await Task.WhenAny(_publishTask, Task.Delay(10000));
                        if (completedTask != _publishTask)
                            Logger.loge("Timeout waiting for _publishTask to complete.");
                        else
                            await _publishTask;
                    }
                    catch (TaskCanceledException)
                    {
                        Logger.logi("PublishLoop cancelled.");
                    }
                    catch (Exception ex)
                    {
                        Logger.loge("Unhandled error in PublishLoop", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.loge("Publish task error", ex);
            }            
            _publishTask = null;

            
            Logger.logi("cancel _commandTask");
            try
            {
                if (_commandTask != null)
                {
                    try
                    {
                        var completedTask = await Task.WhenAny(_commandTask, Task.Delay(1000));
                        if (completedTask != _commandTask)
                        {
                            Logger.loge("Timeout waiting for _commandTask to complete.");
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        Logger.logi("_commandTask cancelled.");
                    }
                    catch (Exception ex)
                    {
                        Logger.loge("Unhandled error in _commandTask", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.loge("_commandTask task error", ex);
            }
            
            _commandTask = null;

           


           
            _repSocket?.Dispose();
            _publisher_socket_msg?.Dispose();
            _publisher_socket_data?.Dispose();            
            _cts?.Dispose();
            _cts = null;

            NetMQConfig.Cleanup();
            
            lock (_lock)
            {                
                _state = ServerState.StateIdle;
                _tcsStopped?.TrySetResult(true);
            }
            Logger.logi("QuoteServer stopped.");
        }

        //correct call sequence:call only after start
        public async Task WaitStopAsync()
        {           
            try
            {                
                await _tcsStopped.Task;
            }
            catch (Exception ex)
            {
                Logger.loge("Error in WaitStopAsync", ex);
            }            
        }

        public ServerState CurrentState
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }



        void QuoteApi_OnLoginResultEvent_DAPI(bool aIsSucc, string aMsg)//登入是否成功
        {
            string sframe = $"[OnLoginResultEvent_DAPI] aIsSucc:{aIsSucc}, aMsg:{aMsg}";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnAnnouncementEvent_DAPI(string data)//公告
        {

            string sframe = $"[OnAnnouncementEvent_DAPI] data:{data}";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnVerifiedEvent_DAPI(TVerifyResult data) //驗證結果
        {
            string sframe = $"[OnVerifiedEvent_DAPI] data.IsSucc:{data.IsSucc}, "
                + $"data.Msg:{data.Msg}, data.MarketKind: {data.MarketKind}";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnSystemEvent_DAPI(SystemEvent data)//訊息
        {
            string sframe = $"[OnSystemEvent_DAPI] data:({data.ToString()})";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnUpdateBasic_DAPI(ProductBasic data)//傳來驗證商品資料
        {
            string sframe = $"[OnUpdateBasic_DAPI] data:({data.ToString()})";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }
        void QuoteApi_OnMatch_DAPI(ProductTick data)//傳來驗證成交行情
        {
            string sframe = $"[OnMatch_DAPI] data:({data.ToString()})";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }

        /// <summary>系統訊息通知</summary>
        private void Observer_OnSystemEvent(SystemEvent data)
        {
            string sframe = $"[OnSystemEvent] data:({data.ToString()})";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);
        }
        TaskCompletionSource<bool> tcs_connect;
        
        /// <summary>系統連線狀態</summary>
        private void Observer_OnConnectState(bool aIsConnected, string aMsg)
        {
            string sframe = $"[OnConnectState] aIsConnected:{aIsConnected}, aMsg:{aMsg}";
            _publisher_socket_msg.SendFrame(sframe);
            Logger.logi(sframe);                        
            if (tcs_connect != null && !tcs_connect.Task.IsCompleted)
                tcs_connect.TrySetResult(aIsConnected);
        }

        bool is_display = false;

        /// <summary>(國內)商品最新快照更新</summary>
        private void Observer_OnUpdateLastSnapshot(ProductSnapshot snapshot)
        {
            if (CurrentState != ServerState.StateRunning)
                return;
            /*
            if (is_display)
            {
                return;
            }
            is_display= true; 
            Logger.logi($"OnSnapshot: {snapshot.BasicData.Symbol}");
            */
            try
            {
                string symbol = snapshot.BasicData.Symbol;
                var tick = snapshot.TickData;

                if (symbol == product_symbol && tick != null) 
                {
                    string ref_price = tick.RefPrice;
                    string price = tick.MatchPrice;
                    string qty = tick.MatchQty;
                    string matchTime = tick.MatchTime;
                    string timestamp = DateTime.UtcNow.Ticks.ToString(); // or .ToString("o") for ISO 8601
                    string message = $"{timestamp}#{symbol}#{matchTime}#{price}#{ref_price}";
                    Logger.logd(message);                    
                    _publisher_socket_data.SendFrame(message);
                }
            }
            catch (Exception ex)
            {
                Logger.loge($"Error in OnSnapshot", ex);
            }
        }
        private async Task PublishLoop(CancellationToken token)
        {        
            tcs_connect = new TaskCompletionSource<bool> ();

            


            //==========================
            Logger.logi("_quoteApi.Login...");
            _quoteApi.Login(user_id, user_password, true); // Sim = true //@@
            Logger.logi("Subscribe  Subscribe100");            
            var completed = await Task.WhenAny(tcs_connect.Task, Task.Delay(20000, token));
            bool is_connected = completed == tcs_connect.Task && tcs_connect.Task.Result;
            if (!is_connected)
            {
                Logger.loge("Can't connect");
                _publisher_socket_msg.SendFrame("[PublishLoop] connect fail");
                return;
            }

            //await Task.Delay(10000);
            Logger.logi("Subscribe  Subscribe200");
            _quoteApi.Subscribe(EXCHANGE.TWF, product_symbol);
            Logger.logi("Subscribe  Subscribe");
            
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token); // 1 second intervalObserver_OnUpdateLastSnapshot
            }

            _quoteApi.Disconnect();


        }

        
    }
}

