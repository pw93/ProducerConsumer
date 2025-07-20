using NetMQ;
using NetMQ;
using NetMQ.Sockets;
using NetMQ.Sockets;
using ProfitWin.Common;
using System.Windows.Forms;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        void pub()
        {
            using (var pubSocket = new PublisherSocket())
            {
                pubSocket.Bind("tcp://*:5556");
                Console.WriteLine("Publisher started.");

                int counter = 0;

                while (true)
                {
                    //pubSocket.SendMoreFrame("TopicA").SendFrame($"Message A {counter}");
                    //pubSocket.SendMoreFrame("TopicB").SendFrame($"Message B {counter}");


                    string original = "username=admin;password=secret";
                    string password = "my-secret-key";

                    string encrypted = CryptoUtils.Encrypt(original, password);
                    //Console.WriteLine("Encrypted code: " + encrypted);

                    //string decrypted = CryptoUtils.Decrypt(encrypted, password);
                    //Console.WriteLine("Decrypted: " + decrypted);

                    pubSocket.SendFrame($"[@@TopicA] {encrypted}");


                    //Console.WriteLine($"pub {counter}");
                    counter++;

                    Thread.Sleep(1000);
                }
            }
        }

        void sub()
        {
            using (var subSocket = new SubscriberSocket())
            {
                subSocket.Connect("tcp://localhost:5556");

                //subSocket.Subscribe("TopicA");  // Only receive TopicA
                subSocket.Subscribe("");  // Only receive TopicA
                Console.WriteLine("Subscriber listening for TopicA...");

                while (true)
                {
                    string msg = subSocket.ReceiveFrameString();
                    //string message = subSocket.ReceiveFrameString();

                    // Remove prefix
                    string prefix = "[@@TopicA]";
                    if (msg.StartsWith(prefix))
                    {
                        string encryptedPayload = msg.Substring(prefix.Length);
                        encryptedPayload = encryptedPayload.Trim();
                        string password = "my-secret-key";

                        // Decrypt it
                        string decrypted = CryptoUtils.Decrypt(encryptedPayload, password);
                        Console.WriteLine($"Decrypted: {decrypted}");
                    }
                    else
                    {
                        Console.WriteLine("Unknown or malformed message");
                    }

                    //string encrypted = CryptoUtils.Encrypt(original, password);
                    //Console.WriteLine("Encrypted code: " + encrypted);

                    //string decrypted = CryptoUtils.Decrypt(encrypted, password);
                    //Console.WriteLine("Decrypted: " + decrypted);

                    //Console.WriteLine($"{decrypted}");
                }
            }

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            //Task.Run(pub);
            //Task.Delay(1000);
            //Task.Run(sub);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string original = "username=admin;password=secret";
            string password = "my-secret-key";

            string encrypted = CryptoUtils.Encrypt(original, password);
            Console.WriteLine("Encrypted code: " + encrypted);

            string decrypted = CryptoUtils.Decrypt(encrypted, password);
            Console.WriteLine("Decrypted: " + decrypted);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Task.Run(pub);
            Task.Delay(1000);
            Task.Run(sub);
        }

        //==========================
        private void AppendMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendMessage(message)));
                return;
            }
            richTextBox1.AppendText(message+"\n");
        }
        private void StartSubscriber(CancellationToken token)
        {
            using var subSocket = new SubscriberSocket();
            subSocket.Connect("tcp://localhost:5556");
            subSocket.Subscribe("StockXYZ");

            AppendMessage("Subscribed to StockXYZ on tcp://localhost:5556");

            while (!token.IsCancellationRequested)
            {
                if (subSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out var msg))
                {
                    AppendMessage("Received: " + msg);
                }
            }

            AppendMessage("Subscriber stopped.");
        }

        private CancellationTokenSource? _cts;
        private Task? _subTask;
        private void button_start_Click(object sender, EventArgs e)
        {

            button_start.Enabled = false;
            button_stop.Enabled = true;
            _cts = new CancellationTokenSource();

            _subTask = Task.Run(() => StartSubscriber(_cts.Token));
            AppendMessage("Subscriber started.");

        }

        

        private void button_stop_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            button_start.Enabled = true;
            button_stop.Enabled = false;
            AppendMessage("Stopping subscriber...");
        }

        private void button_command_Click(object sender, EventArgs e)
        {
            string command = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            Task.Run(() => SendCommandAsync(command));
        }

        private async Task SendCommandAsync(string command)
        {
            try
            {
                using var reqSocket = new RequestSocket();
                reqSocket.Connect("tcp://localhost:5555");

                reqSocket.SendFrame(command);
                string reply = reqSocket.ReceiveFrameString();

                AppendMessage($"Command '{command}' response: {reply}");

                if (command == "shutdown")
                {
                    // Optionally stop subscriber on shutdown
                    _cts?.Cancel();
                    this.Invoke(() =>
                    {
                        button_start.Enabled = true;
                        button_stop.Enabled = false;
                    });
                }
            }
            catch (Exception ex)
            {
                AppendMessage("Command error: " + ex.Message);
            }
        }
    }
}
