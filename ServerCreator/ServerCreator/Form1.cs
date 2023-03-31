using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ServerCreator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        int check = 0;
        private Task task;
        private Process process;
        static async Task Proxy_Start(string targetIP)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:15555/");
            listener.Start();

            while (true)
            {
                var context = await listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    await HandleWebSocketRequest(context, targetIP);
                }
            }
        }



        static async Task HandleWebSocketRequest(HttpListenerContext context, string targetIP)
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var clientWebSocket = webSocketContext.WebSocket;
            var targetTcpClient = new TcpClient();



            try
            {
                await targetTcpClient.ConnectAsync(targetIP, 5555);
            }
            catch (Exception e)
            {
                MessageBox.Show("Timeout on connection with TV, please check TV WiFi " + e.StackTrace);
            }



            var clientWebSocketTask = Task.Run(async () =>
            {
                var buffer = new ArraySegment<byte>(new byte[4096]);
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    var result = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        targetTcpClient.Close();
                        break;
                    }
                    targetTcpClient.GetStream().Write(buffer.Array, 0, result.Count);
                }
            });

            var targetTcpClientTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    int count = await targetTcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length);
                    await clientWebSocket.SendAsync(new ArraySegment<byte>(buffer, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            });

            await Task.WhenAll(clientWebSocketTask, targetTcpClientTask);
        }



        static void sendCommand(string IP)
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "node.exe",
                            Arguments = "websockify.js 15555 " + " " + IP + ":5555",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    Console.WriteLine(process.StandardOutput.ReadToEnd());
                    process.WaitForExit();
                });




            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }



        }




        private async void button1_Click(object sender, EventArgs e)
        {
            string _IP = "";
            Version v = Environment.OSVersion.Version;
            try
            {
                if (textBox1.Text != string.Empty)
                {
                    if (textBox1.TextLength <= 13 && check == 3 || check == 4)
                    {
                        if (v.Major == 6 && v.Minor == 1)
                        {
                            _IP = textBox1.Text;
                            label1.Text = "Server start";
                            label1.Visible = true;

                            sendCommand(_IP);



                        }
                        else
                        {
                            _IP = textBox1.Text;
                            label1.Text = "Server start";
                            label1.Visible = true;

                            await Proxy_Start(_IP);

                        }
                    }
                    else
                    {
                        MessageBox.Show("The IP addres should be 9 digit and need to seperate with comma", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                else
                {
                    MessageBox.Show("Please don't left IP input  ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception k)
            {
                MessageBox.Show(k.Message);
            }


        }


        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //check commas
            if (e.KeyChar == '.')
            {
                check += 1;
            }

            if (check > 5)
            {
                check = 0;
            }


            if ((e.KeyChar < 20) || (e.KeyChar >= '0') && (e.KeyChar <= '9') || (e.KeyChar == '.'))
                return;

            e.Handled = true;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (process != null)
                {
                    process.Kill();
                    MessageBox.Show("Task ended with no problem");

                }

            }
            catch (Exception k)
            {
                MessageBox.Show(k.Message);
            }

            var nodeRunProcess = Process.GetProcessesByName("node");


            foreach (var nodePro in nodeRunProcess)
            {
                try
                {
                    nodePro.Kill();
                    label1.Text = "Stopped";
                    MessageBox.Show("Task ended -- Node");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }
    }
}
