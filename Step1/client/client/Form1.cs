using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {
        bool terminating = false;
        bool connected = false;
        Socket clientSocket;
        string name = "";
        int currentQuestion = 0;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        // handle closing of the form
        // set connection boolean FALSE, and start termiantion by set termiantion TRUE
        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            clientSocket.Close();
            Environment.Exit(0);
        }

        // send given message from users socket
        private void sendMessage(Socket socket, string message)
        {
            if (message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
            }
        }

        // recieve message to given socket and encode it to readable message and return that message
        private string receiveMessage(Socket socket)
        {
            lock (this)
            {
                Byte[] buffer = new Byte[1024];
                socket.Receive(buffer);
                string message = Encoding.Default.GetString(buffer);
                message = message.Substring(0, message.IndexOf("\0"));
                return message;
            }
        }


        private void buttonConnect_Click(object sender, EventArgs e)
        {
            //set new socket to client and get IP from IP text box in form
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBoxIp.Text;

            int portNum;
            if (Int32.TryParse(textBoxPort.Text, out portNum)) // check if entered port is integer
            {
                try
                {
                    IPAddress ipAd = IPAddress.Parse(IP); // cpnvert ip address to integer
                    clientSocket.Connect(ipAd, portNum); // connect client socket with provided ip and port numbers
                    sendMessage(clientSocket, textBoxName.Text); // send client name to server
                    string reply = receiveMessage(clientSocket); // receive servers response to our sent name
                    if (reply.Contains("This name is already in use")) // if name already exist in game close socket
                    {
                        logs.AppendText(reply + "\n");
                        clientSocket.Close();
                    }

                    else if (reply.Contains("running quiz")) // if a game is alreadt started/running at the server
                    {
                        logs.AppendText(reply + "\n");
                        clientSocket.Close();
                    }
                    else // if user is okay to join game
                    {
                        // disable input buttons/boxes of form and enable disconnect button
                        buttonConnect.Enabled = false;
                        buttonDisconnect.Enabled = true;
                        textBoxIp.Enabled = false;
                        textBoxPort.Enabled = false;
                        textBoxName.Enabled = false;
                        connected = true;

                        logs.AppendText("Connected to the server!\n");
                        logs.AppendText("Waiting for the quiz to start...\n");

                        // create and start a recieve thread to get messages from server
                        Thread receiveThread = new Thread(Receive);
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                    }
                }
                catch // if problem occured while connecting to the game
                {
                    logs.AppendText("Could not connect to the server!\n");
                }
            }
            else // if entered port is not integer
            {
                logs.AppendText("Check the port\n");
            }
        }

        // process message string
        // send to user and enable user input for the answer
        private void handleQuestion(string message)
        {
            lock (this)
            {
                // split question message to question number and question string
                string[] splittedMessage = message.Split(':');
                string qNumber = splittedMessage[0];
                currentQuestion = Int32.Parse(qNumber); // convert question number to integer
                string question = splittedMessage[1];
                // print question on users log
                logs.AppendText("Question " + (currentQuestion + 1).ToString() + " - " + question + "\n");
                // enable answer box and send button
                textBoxAnswer.Enabled = true;
                buttonSend.Enabled = true;
            }
        }

        // recieve messages/questions from server and print to user log
        private void Receive()
        {
            while (connected)
            {
                try
                {
                    string incomingMessage = receiveMessage(clientSocket);
                    if (incomingMessage.Contains(":")) // if incoming message is quesiton
                    {
                        string[] splittedMessage = incomingMessage.Split(':');
                        string qNumber = splittedMessage[0]; // get question number
                        int qnum;
                        if (Int32.TryParse(qNumber, out qnum)) // if question number is integer
                        {
                            handleQuestion(incomingMessage); // process the question and send question to user
                        }
                        else // if question number not integer, message is not a question, print message to user
                        {
                            logs.AppendText(incomingMessage + "\n");
                        }

                    }
                    else if (incomingMessage != "") // if incoming message is not a question and not empty, print to user log
                    {
                        logs.AppendText("Server: \n" + incomingMessage + "\n");
                    }
                }
                catch
                { // if porblem occurs processing the incoming message, means connection lost with the server

                    if (!terminating) // if not terminating the client form
                    {
                        // print server is disconnected to user and enable buttons and text boxes required to connect again
                        logs.AppendText("The server has disconnected\n");
                        buttonConnect.Enabled = true;
                        buttonDisconnect.Enabled = false;
                        textBoxIp.Enabled = true;
                        textBoxPort.Enabled = true;
                        textBoxName.Enabled = true;
                    }
                    // close the users socket and set connection status FALSE
                    clientSocket.Close();
                    connected = false;
                }

            }
        }

        // handler of Send Button clicked
        private void buttonSend_Click(object sender, EventArgs e)
        {
            // send answer message with the current question to the server
            string message = currentQuestion.ToString() + ":" + textBoxAnswer.Text;
            sendMessage(clientSocket, message);
            // disable answer and send buttons
            textBoxAnswer.Enabled = false;
            buttonSend.Enabled = false;
        }

        // handler of disconnect button clicked
        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            // close the user socket and reset all buttons and textboxes in the form to initial version
            clientSocket.Close();
            connected = false;
            buttonConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            textBoxIp.Enabled = true;
            textBoxPort.Enabled = true;
            textBoxName.Enabled = true;
            textBoxAnswer.Enabled = false;
            buttonSend.Enabled = false;
        }
    }
}

