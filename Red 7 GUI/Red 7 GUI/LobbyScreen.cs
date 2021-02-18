﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Red_7_GUI
{
    public partial class LobbyScreen : Form
    {
        private bool host;
        private string username;
        private IPAddress ip;
        private bool advanced;
        private bool actionRule;
        private int port = 56565;
        public string receive;
        public string send = "";
        private bool gameStarted;
        public LobbyScreen(bool host, IPAddress ip, string username)
        {
            this.host = host;
            this.username = username;
            clientPlayers = new string[4];
            gameStarted = false;
            InitializeComponent();
            startButton.Enabled = false;

            if (!host)
            {
                this.ip = ip;
                startServerButton.Enabled = false;
                actionCheckBox.Enabled = false;
                advancedCheckBox.Enabled = false;
                

                if (!ConnectToServer())
                {
                    Close();
                }

                ClientSend("0" + username);

                receiver.RunWorkerAsync();//starts receiver
            }
            else
            {
                IPHostEntry hostInfo = Dns.GetHostEntry("localhost");
                this.ip = hostInfo.AddressList[hostInfo.AddressList.Length - 1];
                player0Label.Text = username + " (host)";
            }

            //MessageBox.Show(this.ip.ToString());
        }
        #region Lobby
        private void UpdateLabels()
        {
            //ensures that this is called from the main thread - winforms does not support multithreaded access to components
            this.player0Label.Invoke((MethodInvoker)delegate {
                this.player0Label.Text = clientPlayers[0] + " (host)";
            });
            this.player1Label.Invoke((MethodInvoker)delegate {
                this.player1Label.Text = clientPlayers[1];
            });
            this.player2Label.Invoke((MethodInvoker)delegate {
                this.player2Label.Text = clientPlayers[2];
            });
            this.player3Label.Invoke((MethodInvoker)delegate {
                this.player3Label.Text = clientPlayers[3];
            });
        }
        private void UpdateRules()
        {
            this.advancedCheckBox.Invoke((MethodInvoker)delegate {
                this.advancedCheckBox.Checked = advanced;
            });
            this.actionCheckBox.Invoke((MethodInvoker)delegate {
                this.actionCheckBox.Checked = actionRule;
            });
        }
        private void helpButton_Click(object sender, EventArgs e)
        {
            //open lobby help window
        }
        private void quitButton_Click(object sender, EventArgs e)
        {
            if (host)
            {
                numClients = 0;
                serverReceiver.CancelAsync();
                Thread.Sleep(200);
                clients = default;
                readers = default;
                writers = default;
                serverPlayers = default;
            }
            else
            {
                MessageBox.Show("sending leave message");
                ClientSend("1");
            }
            Close();
        }
        private void startButton_Click(object sender, EventArgs e)
        {
            Random rnd = new Random();
            int seed = rnd.Next(0, 2147483647);//generates a random seed for the game
            FindFirstPlayer(seed);//finds the starting player

            //start game
        }
        private void actionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            actionRule = actionCheckBox.Checked;
            ServerDecode("2", 0);//tells the server that rules have changed
        }
        private void advancedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            advanced = advancedCheckBox.Checked;
            ServerDecode("2", 0);//tells the server that rules have changed
        }
        #endregion
        #region Client
        private StreamReader STR;
        private StreamWriter STW;
        private TcpClient client;
        private string[] clientPlayers;
        private int numPlayers = 0;
        private bool ConnectToServer()
        {
            client = new TcpClient();
            IPEndPoint remoteEndpoint = new IPEndPoint(ip, port);

            try
            {
                client.Connect(remoteEndpoint);
                if (client.Connected)
                {
                    //MessageBox.Show("Connected to server");
                    STW = new StreamWriter(client.GetStream());
                    STR = new StreamReader(client.GetStream());
                    STW.AutoFlush = true;

                    return true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }

            return false;
        }
        private void ClientSend(string data)
        {
            if (client.Connected)
            {
                STW.WriteLine(data);
                //MessageBox.Show("Sent " + data);
            }
        }
        private void receiver_DoWork(object sender, DoWorkEventArgs e)
        {
            while (client.Connected)
            {
                try
                {
                    receive = STR.ReadLine();
                    if (receive != "")
                    {
                        MessageBox.Show("Received " + receive);
                        ClientDecode(receive);
                        receive = "";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        private void ClientDecode(string data)//triggers when a player joins the lobby
        {
            switch (data[0])
            {
                case '0'://join (username)
                    clientPlayers = data.Substring(1).Split('~');
                    numPlayers++;
                    UpdateLabels();
                    break;
                case '1'://leave
                    MessageBox.Show("client received leave message");
                    try
                    {
                        CPlayerLeft((int)data[1]);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("Leaving player not integer");
                    }
                    break;
                case '2'://rule update
                    if (data[1] == '0')//advanced
                    {
                        advanced = false;
                    }
                    else
                    {
                        advanced = true;
                    }
                    if (data[2] == '0')
                    {
                        actionRule = false;
                    }
                    else
                    {
                        actionRule = true;
                    }
                    UpdateRules();
                    break;
                case '3'://end turn
                    break;
                case '4'://start game
                    break;
                case '5'://
                    break;
                default:
                    MessageBox.Show("Invalid transmission type at server");
                    break;
            }
        }
        private void CPlayerLeft(int player)
        {
            numPlayers--;
            for (int i = player; i < numPlayers; i++)
            {
                clientPlayers[i] = clientPlayers[i + 1];
            }
            UpdateLabels();
        }
        #endregion
        #region Server
        private TcpClient[] clients;
        private StreamReader[] readers;
        private StreamWriter[] writers;
        private string[] serverPlayers;
        private int numClients;
        private int currentPlayerIndex;
        private void StartServer()
        {
            serverPlayers = new string[4];
            clients = new TcpClient[4];
            readers = new StreamReader[4];
            writers = new StreamWriter[4];

            listener.RunWorkerAsync();
            serverReceiver.RunWorkerAsync();

            ConnectToServer();//connects host to the server
            receiver.RunWorkerAsync();
            numPlayers = 1;

            ClientSend("0" + username);
        }
        private void startServerButton_Click(object sender, EventArgs e)
        {
            numClients = 0;
            startButton.Enabled = true;
            startServerButton.Enabled = false;
            StartServer();
        }
        private void listener_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            while (numClients < 4 && !gameStarted)
            {
                clients[numClients] = listener.AcceptTcpClient();
                readers[numClients] = new StreamReader(clients[numClients].GetStream());
                writers[numClients] = new StreamWriter(clients[numClients].GetStream());
                writers[numClients].AutoFlush = true;
                numClients++;
            }
            listener.Stop();
            listener = null;
        }
        private void ServerDecode(string data, int clientNum)//triggers when a player joins the lobby
        {
            string msg;
            string msg2;
            switch (data[0])
            {
                case '0'://join (username)
                    serverPlayers[clientNum] = data.Substring(1);

                    msg = "0";

                    foreach(string name in serverPlayers)
                    {
                        msg += name + "~";
                    }

                    for (int i = 0; i < numClients; i++)
                    {
                        if (clients[i].Connected)
                        {
                            //MessageBox.Show("sending " + msg + " to " + i.ToString());
                            writers[i].WriteLine(msg);//sends data to client i
                        }
                    }

                    msg2 = "2";

                    if (advanced)
                    {
                        msg2 += "1";
                    }
                    else
                    {
                        msg2 += "0";
                    }
                    if (actionRule)
                    {
                        msg2 += "1";
                    }
                    else
                    {
                        msg2 += "0";
                    }

                    for (int i = 1; i < numClients; i++)//send to all except host
                    {
                        if (clients[i].Connected)
                        {
                            //MessageBox.Show("sending " + msg2 + " to " + i.ToString());
                            writers[i].WriteLine(msg2);//sends data to client i
                        }
                    }
                    break;
                case '1'://leave
                    MessageBox.Show("server received leave message");
                    SPlayerLeft(clientNum);
                    msg = "1" + clientNum.ToString();

                    for (int i = 0; i < numClients; i++)
                    {
                        if (clients[i].Connected)
                        {
                            //MessageBox.Show("sending " + msg + " to " + i.ToString());
                            writers[i].WriteLine(msg);//sends data to client i
                        }
                    }
                    break;
                case '2'://rule update
                    msg = "2";

                    if (advanced)
                    {
                        msg += "1";
                    }
                    else
                    {
                        msg += "0";
                    }
                    if (actionRule)
                    {
                        msg += "1";
                    }
                    else
                    {
                        msg += "0";
                    }

                    for (int i = 1; i < numClients; i++)//send to all except host
                    {
                        if (clients[i].Connected)
                        {
                            //MessageBox.Show("sending " + msg2 + " to " + i.ToString());
                            writers[i].WriteLine(msg);//sends data to client i
                        }
                    }
                    break;
                case '3'://end turn
                    break;
                case '4'://
                    break;
                default:
                    MessageBox.Show("Invalid transmission type at server");
                    break;
            }
        }
        private void serverReceiver_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                //MessageBox.Show("looping");
                for (int i = 0; i < numClients; i++)
                {
                    if (clients[i].Connected)
                    {
                        //MessageBox.Show("Client " + i.ToString() + " is connected");
                        try
                        {
                            if (readers[i].Peek() != -1)
                            {
                                //MessageBox.Show("data detected");
                                receive = readers[i].ReadLine();
                                MessageBox.Show(receive + " from " + i.ToString());
                                ServerDecode(receive, i);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            }
        }
        private void SPlayerLeft(int player)//removes a players connection server-side
        {
            numClients--;
            for (int i = player; i < numClients; i++)//moves all items after the player back one
            {
                clients[i] = clients[i + 1];
                readers[i] = readers[i + 1];
                writers[i] = writers[i + 1];
            }
        }
        private void FindFirstPlayer(int seed)
        {
            Deck deck = new Deck();
            deck.Reset(seed);
            Card lowestCard = new Card(7, 7);
            int startingPlayer = -1;

            for (int i = 0; i < numClients; i++)
            {
                if (deck.GetCard(i).GetScore() < lowestCard.GetScore())
                {
                    lowestCard = deck.GetCard(i);
                    startingPlayer = i;
                }
            }

            if (startingPlayer == -1)
            {
                throw new Exception("lowest card not found - no card lower than red 7");
            }

            currentPlayerIndex = startingPlayer;
        }
        #endregion
    }
}
