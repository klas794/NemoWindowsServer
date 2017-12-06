using NemoServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NemoWindowsServerXaml2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private int _currentOrderId { get; set; }

        NetworkStream stream;
        //  task = new Task(() => Listen());
        //Task task = new Task(() => FromClient());
        TcpClient client;

        public MainWindow()
        {
            InitializeComponent();

            Task task = new Task(StartUpServer);
            task.Start();

            var grid = new GridView();

            ListViewOrders.View = grid;

            grid.Columns.Add(new GridViewColumn
            {
                Header = "Id",
                DisplayMemberBinding = new Binding("OrderNumber"),
                Width = 33
            });

            grid.Columns.Add(new GridViewColumn
            {
                Header = "Namn",
                DisplayMemberBinding = new Binding("DishName"),
                Width = 80
            });

            grid.Columns.Add(new GridViewColumn
            {
                Header = "Tidpunkt",
                DisplayMemberBinding = new Binding("OrderTime"),
                Width = 80
            });

            //ListViewOrders.Items.Add(new Order { OrderNumber = 1, DishName = "Davids palsternacka" });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selection = ListViewOrders.SelectedItem as Order;

            if(selection != null)
            {
                var message = String.Format("Order id {0}: {1} ready.", selection.OrderNumber, selection.DishName);

                Dispatcher.Invoke(() => {
                    Output.Text = message;
                });

                ToClient(message);

                ListViewOrders.Items.Remove(selection);
            }
        }

        public void StartUpServer()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
            server.Start();
            Dispatcher.Invoke(() => {
                Output.Text = String.Format("Server has started on 127.0.0.1:8080.{0}Waiting for a connection...", Environment.NewLine);
            });

            while (true)
            {
                client = server.AcceptTcpClient();
                Dispatcher.Invoke(() => {
                    Output.Text = ("A client connected.");
                });
                stream = client.GetStream();
                //enter to an infinite cycle to be able to handle every change in stream

                while (!stream.DataAvailable) ;
                Byte[] bytes = new Byte[client.Available];

                stream.Read(bytes, 0, bytes.Length);

                //translate bytes of request to string
                String data = Encoding.UTF8.GetString(bytes);

                //if (Regex.IsMatch(data, "^GET"))
                //{

                //}
                if (new Regex("^GET").IsMatch(data))
                {
                    //Console.WriteLine("New connection established");
                    Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                        + "Connection: Upgrade" + Environment.NewLine
                        + "Upgrade: websocket" + Environment.NewLine
                        + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                            SHA1.Create().ComputeHash(
                                Encoding.UTF8.GetBytes(
                                    new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                )
                            )
                        ) + Environment.NewLine
                        + Environment.NewLine);

                    stream.Write(response, 0, response.Length); //Avsluta handskakningen
                                                                // task.Start();
                    FromClient();
                }
                else
                {

                }

            }
        }

        void FromClient()
        {

            while (true)
            {
                // Console.WriteLine("FromClien");
                var bytes = new Byte[1024];

                try
                {
                    int rec = stream.Read(bytes, 0, 1024);  //Blocking

                    var length = bytes[1] - 128; //message length
                    Byte[] key = new Byte[4];
                    Array.Copy(bytes, 2, key, 0, key.Length);
                    byte[] encoded = new Byte[length];
                    byte[] decoded = new Byte[length];
                    Array.Copy(bytes, 6, encoded, 0, encoded.Length);
                    for (int i = 0; i < encoded.Length; i++)
                    {
                        decoded[i] = (Byte)(encoded[i] ^ key[i % 4]);
                    }
                    var data = Encoding.UTF8.GetString(decoded);

                    if (data == "" || data.StartsWith("\u0003"))
                        continue;

                    _currentOrderId++;
                    Dispatcher.Invoke(() => {
                        ListViewOrders.Items.Add(new Order { OrderNumber = _currentOrderId, DishName = data });
                        //var view = CollectionViewSource.GetDefaultView(ListViewOrders.Items);
                        //view.Refresh();
                    });

                    Dispatcher.Invoke(() =>
                    {
                        Output.Text = String.Format("Order #{0} : {1} added", _currentOrderId, data);
                    });
                    if (data == "exit") break;
                    if (!string.IsNullOrEmpty(data))
                    {
                        ToClient(data + " ordered. Your #:" + _currentOrderId);
                    }
                }
                catch (Exception e)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Output.Text = "Client disconnected";
                    });

                    break;
                }
            }
            stream.Close();
            client.Close();
        }

        void ToClient(string input)
        {
            var s = input;
            var message = Encoding.UTF8.GetBytes(s);
            var send = new byte[message.Length + 2];
            send[0] = 0x81;
            send[1] = (byte)(message.Length); //Datal�ngd dvs antal bytes
            for (var i = 0; i < message.Length; i++)
            {
                send[i + 2] = (byte)message[i];
            }
            //byte[] send = new byte[3 + 2];
            //send[0] = 0x81; // last frame, text
            //send[1] = 3; // not masked, length 3
            //send[2] = 0x41;
            //send[3] = 0x42;
            //send[4] = 0x43;

            try { 
                stream.Write(send, 0, send.Length);
            }
            catch(Exception)
            {
                Output.Text = "Client disconnected";
            }


        }

    }
}
