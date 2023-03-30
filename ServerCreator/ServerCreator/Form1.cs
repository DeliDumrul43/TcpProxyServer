using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Net.Sockets;
using System.Net;
using System.CodeDom;

namespace ServerCreator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var proxy = new TcpProxyServer("10.100.5.120", 5555);
            //var ws = new WebSocketServer("ws://localhost:15555");


            //  ws.Start();
            proxy.Start();
            label1.Text = "Server started";
            
        }




        class TcpProxyServer
        {
            private readonly string _targetAddress;
            private readonly int _targetPort;

            public TcpProxyServer(string targetAddress, int targetPort)
            {
                
                _targetAddress = targetAddress;
                _targetPort = targetPort;
            }

            public void Start()
            {
                var listener = new TcpListener(IPAddress.Loopback, 15555);
                listener.Start();
                listener.BeginAcceptTcpClient(OnClientConnect, listener);
            }

            private void OnClientConnect(IAsyncResult result)
            {
                var listener = (TcpListener)result.AsyncState;
                var client = listener.EndAcceptTcpClient(result);
                var target = new TcpClient(_targetAddress, _targetPort);
                listener.BeginAcceptTcpClient(OnClientConnect, listener);
                var forwarder = new TcpForwarder(client.GetStream(), target.GetStream());
                forwarder.Start();
            }
        }
        class TcpForwarder
        {
            private readonly NetworkStream _incoming;
            private readonly NetworkStream _outgoing;
            public TcpForwarder(NetworkStream incoming, NetworkStream outgoing)
            {
                _incoming = incoming;
                _outgoing = outgoing;
            }

            public void Start()
            {
                var incomingBuffer = new byte[4096];
                _incoming.BeginRead(incomingBuffer, 0, incomingBuffer.Length, OnIncomingData, incomingBuffer);
                var outgoingBuffer = new byte[4096];
                _outgoing.BeginRead(outgoingBuffer, 0, outgoingBuffer.Length, OnOutgoingData, outgoingBuffer);

            }

            private void OnIncomingData(IAsyncResult result)
            {
                var buffer = (byte[])result.AsyncState;

                if (_incoming.CanRead && _incoming.DataAvailable)
                {

                    var bytesRead = _incoming.EndRead(result);

                    if (bytesRead > 0)
                    {
                        _outgoing.Write(buffer, 0, bytesRead);
                        _incoming.BeginRead(buffer, 0, buffer.Length, OnIncomingData, buffer);
                    }
                }
                
            }
            private void OnOutgoingData(IAsyncResult result)
            {
                var buffer = (byte[])result.AsyncState;
                var bytesRead = _outgoing.EndRead(result);
                if (_outgoing.CanRead && _outgoing.DataAvailable)
                {

                    if (bytesRead > 0)
                    {
                        _incoming.Write(buffer, 0, bytesRead);
                        _outgoing.BeginRead(buffer, 0, buffer.Length, OnOutgoingData, buffer);
                    }
                }
                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string _IP = "";

            if (textBox1.Text != string.Empty)
            {
                _IP = textBox1.Text;

            }
        }
    }
}
