using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server
{

    public struct player
    {
        public int id;
        public string name;
        public double points;
        public Socket socket;

        public player(int id, string name, Socket socket)
        {
            this.id = id;
            this.name = name;
            this.points = 0.0;
            this.socket = socket;
        }

        // add given point to the player
        public player addPoint(double point)
        {
            this.points = this.points + point;
            return this;
        }

        // sets users points to 0
        public player zeroPoint()
        {
            this.points = 0;
            return this;
        }
    }

    public struct question
    {
        public string questionString;
        public int answer;

        public question(string questionString, int answer)
        {
            this.questionString = questionString;
            this.answer = answer;
        }
    }

    public partial class Form1 : Form
    {
        // required declarations
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<player> users = new List<player>();
        List<question> questions = new List<question>();
        int[] answers;

        int numOfQuestions = 0;
        bool terminating = false;
        bool listening = false;
        bool isEnoughUser = false;
        int answeredUserCount = 0;
        int currentQuestionNumber = 0;
        int idCount = 0;
        bool isQuizStarted = false;
        int disconnectedCount = 0;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }
        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            serverSocket.Close();
            Environment.Exit(0);
        }

        private void buttonListen_Click(object sender, EventArgs e)
        {
            int serverPort;

            // check if port written by user is integer if so, export it to serverPort
            if (Int32.TryParse(textBoxPort.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(999); // can listen up to 999 users

                // after approptiate port entered, input textboxes and listen button become unavailable
                listening = true;
                textBoxPort.Enabled = false;
                textBoxNumberOfQuestions.Enabled = false;
                buttonListen.Enabled = false;


                Thread acceptThread = new Thread(Accept);
                acceptThread.IsBackground = true;
                acceptThread.Start();

                richTextBoxLogs.AppendText("Started listening on port " + serverPort + "\n");
                Int32.TryParse(textBoxNumberOfQuestions.Text, out numOfQuestions); // convert num of questions to integer
            }
            else // appropriate error message for port number
            {
                richTextBoxLogs.AppendText("Please check port number \n");
            }
        }

        // check if a user with given name already connected to the game
        private bool isNameExists(string newName)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (newName == users[i].name)
                {
                    return true;
                }
            }
            return false;
        }

        // read questions from the given text file
        private void readQuestionsTxt()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\questions.txt");
            List<string> lines = File.ReadLines(path).ToList();
            for (int i=0; i<lines.Count; i=i+2)
            {
                question newQuestion = new question(lines[i], Int32.Parse(lines[i + 1]));
                questions.Add(newQuestion);
            }
        }


        // terminate the ongoing quiz
        private void terminateQuiz()
        {
            // enable buttons and input text boxes
            textBoxPort.Enabled = true;
            textBoxNumberOfQuestions.Enabled = true;
            buttonListen.Enabled = true;
            isQuizStarted = false;

            // close each users sockets
            for (int k = 0; k < users.Count; k++)
            {
                users[k].socket.Close();
            }

            // delete/clear all users and questions and delete servers socket
            users.Clear();
            questions.Clear();
            serverSocket.Close();

            // create new socket for the server
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            numOfQuestions = 0;
            terminating = false;
            listening = false;
            isEnoughUser = false;
            answeredUserCount = 0;
            currentQuestionNumber = 0;
            idCount = 0;
            isQuizStarted = false;
            disconnectedCount = 0;
        }

        // accept players trying to connect
        private void Accept()
        {
            while (listening)
            {
                try
                {
                    while(!isEnoughUser) // while user number is not equal to 2, keep accepting users
                    {

                        Socket newClient = serverSocket.Accept();
                        Byte[] buffer = new Byte[1024];
                        newClient.Receive(buffer);
                        string nameOfClient = Encoding.Default.GetString(buffer);
                        nameOfClient = nameOfClient.Substring(0, nameOfClient.IndexOf("\0"));
                        
                        if (!isNameExists(nameOfClient)) //check name of user is unique
                        {
                            //Create new player and add to the users list
                            player newUser = new player(idCount, nameOfClient, newClient);
                            users.Add(newUser);
                            idCount++;
                            sendMessage(newClient, "Connection Success!");

                            // start listening from the user by recieve thread
                            Thread receiveThread = new Thread(() => Receive(newUser));
                            receiveThread.IsBackground = true;
                            receiveThread.Start();
                            //log connected user
                            richTextBoxLogs.AppendText(nameOfClient + " is connected.\n");
                            // condition update
                            if (users.Count == 2)
                            {
                                isEnoughUser = true;
                            }
                        }
                        else // if player trying to connect doesnt have a unique name show error
                        {
                            richTextBoxLogs.AppendText(nameOfClient + " tries to connect but this name already connected.\n");
                            sendMessage(newClient, "Connection Failed! This name is already in use. Try with another name.");
                            newClient.Close();
                        }
                    }

                    // --------------------------------------------------------------------------------//
                    Thread externalUserThread = new Thread(externalUser);
                    externalUserThread.IsBackground = true;
                    externalUserThread.Start();

                    // if quiz not started
                    if (!updateOrGetIsQuizStarted(1))
                    {
                        answers = new int[users.Count]; // create answers array
                        updateOrGetIsQuizStarted(3); // set isQuizStarted as TRUE
                        readQuestionsTxt(); // read the questions from txt
                        Quiz(); // start the quiz
                        terminateQuiz(); // finish the quiz
                    }
                }
                catch
                {
                    // set listening as false if it is terminating the form
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        richTextBoxLogs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }


        // don't allow an external player to join game if there is already a game going on and show appropriate messages
        private void externalUser()
        {
            while (listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    sendMessage(newClient, "Connection Failed! There is a running quiz right now!");
                    newClient.Close();
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        richTextBoxLogs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        // get messages from a socket as a string
        private string receiveMessage(Socket socket)
        {
            Byte[] buffer = new Byte[1024];
            socket.Receive(buffer);
            string message = Encoding.Default.GetString(buffer);
            message = message.Substring(0, message.IndexOf("\0"));
            return message;
        }

        // send the given message to the socket provided
        private void sendMessage(Socket socket, string message)
        {
            if (message != "" && message.Length <= 1024)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                try
                {
                    socket.Send(buffer);
                }
                catch
                {
                    richTextBoxLogs.AppendText("There is a problem in sendMessage! Check the connection...\n");
                    terminating = true;
                    textBoxPort.Enabled = true;
                    buttonListen.Enabled = true;
                    serverSocket.Close();
                }
            }
            Thread.Sleep(1000);
        }

        // process the answer string such that, it splits to question number and answer
        // check validity of an answer, save it if its valid, show error if not valid
        // also check if answers is an answer for the currently asked questions
        private void proccessAnswer(ref player player, ref bool connected, ref string message)
        {
            lock (this)
            {
                string[] splittedMessage = message.Split(':');
                string qNumber = splittedMessage[0];
                string answerString = splittedMessage[1];
                int answer;
                // check if answer is integer
                if(Int32.TryParse(answerString, out answer))
                {
                    // check if answer is the answer for the current question
                    if (qNumber == currentQuestionNumber.ToString())
                    {
                        sendMessage(player.socket, "Your answer for Question "+ (currentQuestionNumber+1)+ " is received! Waiting for other user to answer...");
                        // save answer
                        answers[player.id] = answer;
                        updateAnsweredUserCount(1);
                    }
                }
                else
                {
                    sendMessage(player.socket, "Invalid answer! Your answer should be numeric");
                }
            }
        }


        // recieve input/answer from the player
        private void Receive(player player)
        {
            bool connected = true; // set user as connected

            while (connected && !terminating)
            {
                try // try to recieve a message from the player, if recieves, process the message
                {
                    string message = receiveMessage(player.socket);
                    if (message.Contains(':'))
                    {
                        proccessAnswer(ref player, ref connected, ref message);
                    }
                }
                catch // cannot recieve from user, therefore user disconnected(left the app or lost connection)
                {
                    if (!terminating) // if program is terminating, count the disconnected users
                    {
                        richTextBoxLogs.AppendText(player.name+" has disconnected\n");
                        disconnectedCount += 1;
                    }
                    // close the players socket and set connected status false, and wait for program to do same for other player(s)
                    player.socket.Close();
                    connected = false;
                    Thread.Sleep(1000);
                    if (updateOrGetIsQuizStarted(1) && disconnectedCount != 2) // if quiz is started and 2 players did not disconnected beacuse of termination of program (some player disconnected ingame)
                    {
                        // for each player left in game, inform them they win the game and finish quiz
                        foreach (player user in users) 
                        {
                            if (user.id != player.id)
                            {
                                sendMessage(user.socket, "The other player is disconnected. You win the game!");
                                users[player.id] = player.zeroPoint(); // make each users points 0
                                sendMessage(user.socket, getScores()); // send each user the score table
                                user.socket.Close(); // close each users sockets
                                users.Remove(user); // delete the user
                                users.Remove(player); // delete the player
                                terminateQuiz(); // finish the quiz appropriately
                                break;
                            }
                        }
                    }
                }
            }
        }

        // get the current points and return the score table as string
        private string getScores()
        {
            string result = "Score Table\n";
            var sortedList = users.OrderByDescending(x => x.points);
            foreach (player user in sortedList)
            {
                result += user.name + " - " + user.points.ToString() + "\n";
            }
            return result;
        }

        // find and return who is the winner users index, return -1 if game is tie
        private int getWinner()
        {
            if (users[0].points > users[1].points)
            {
                return 0;
            }
            else if (users[1].points > users[0].points)
            {
                return 1;
            }
            else return -1;
        }

        // send the given question to the user 
        // if cannot sent, show error, termiante the process and close server socket
        private void sendQuestion(string question, player user)
        {
            if (currentQuestionNumber < numOfQuestions)
            {
                Byte[] buffer = Encoding.Default.GetBytes(currentQuestionNumber.ToString() + ":" + question);
                try
                {
                    richTextBoxLogs.AppendText((currentQuestionNumber + 1).ToString() + ". question sent to " + user.name + "\n");
                    user.socket.Send(buffer);
                }
                catch
                {
                    richTextBoxLogs.AppendText("There is a problem in! Check the connection...\n");
                    terminating = true;
                    textBoxPort.Enabled = true;
                    buttonListen.Enabled = true;
                    serverSocket.Close();
                }
            }
        }

        // compare 2 users answers with true answer and return the users index who is closer to true answer, return -1 if it is tie
        private int compareResult(int firstUserAnswer, int secondUserAnswer, int actualResult)
        {
            if (Math.Abs(actualResult - firstUserAnswer) < Math.Abs(actualResult - secondUserAnswer))
            {
                return 0;
            }
            else if (Math.Abs(actualResult - firstUserAnswer) > Math.Abs(actualResult - secondUserAnswer))
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        // update the number of users answered the question by i
        private void updateAnsweredUserCount(int i)
        {
            lock (this)
            {
                answeredUserCount += i;
            }
        }

        // increment the current quesitons number by one
        private void incrementCurrentQuestionNumber()
        {
            lock (this)
            {
                currentQuestionNumber += 1;
            }
        }


        // depending on parameter a, return if quiz is started, OR set state of quiz being started as FALSE and return if quiz is started, OR set state of quiz being started as TRUE and return if quiz is started
        // if choice is not 1,2 or 3, return if quiz is started (isQuizStarted)
        private bool updateOrGetIsQuizStarted(int a)
        {
            lock (this)
            {
                if (a == 1) //get isQuizStarted
                {
                    return isQuizStarted;
                }
                else if (a == 2) //make isQuizStarted False
                {
                    isQuizStarted = false;
                    return isQuizStarted;
                }
                else if (a == 3) //make isQuizStarted True
                {
                    isQuizStarted = true;
                    return isQuizStarted;
                }
                else
                {
                    return isQuizStarted;
                }
            }
            
        }

        // Starts the quiz game
        private void Quiz()
        {

            if (updateOrGetIsQuizStarted(1) && currentQuestionNumber < numOfQuestions) // update quiz started as TRUE, and if all questions not asked
            {
                for (int i = 0; i < numOfQuestions; i++) // iterate over all questions
                {
                    // send question to the all users
                    int userCount = users.Count;
                    for (int k = 0; k < userCount; k++) 
                    {
                        sendQuestion(questions[i % questions.Count].questionString, users[k]);
                    }

                    // print question the the server log
                    richTextBoxLogs.AppendText("Question - "+ (i+1).ToString() +" "+ questions[i % questions.Count].questionString + "\n\n");

                    // Wait if all users did not answer
                    while (answeredUserCount != userCount)
                    {
                        ;
                    }

                    // if all users answered
                    if (answeredUserCount == userCount)
                    {
                        // send all users answers and the correct answer to the each user and server log
                        for (int k = 0; k < userCount; k++)
                        {
                            sendMessage(users[k].socket, users[0].name + "\'s answer is " + answers[0].ToString() + "\n" +
                                                            users[1].name + "\'s answer is " + answers[1].ToString() + "\n" +
                                                            "The correct answer was " + questions[i % questions.Count].answer.ToString() + "\n");
                            
                        }

                        richTextBoxLogs.AppendText(users[0].name + "\'s answer is " + answers[0].ToString() + "\n" +
                                                            users[1].name + "\'s answer is " + answers[1].ToString() + "\n" +
                                                            "The correct answer was " + questions[i % questions.Count].answer.ToString() + "\n\n");

                        // decide who win that round, distribute appropriate points to users with appropriate message
                        int result = compareResult(answers[0], answers[1], questions[i % questions.Count].answer);
                        if (result == 0) // if first user won
                        {
                            users[0] = users[0].addPoint(1);
                            sendMessage(users[0].socket, "You win! You get 1 point\n");
                            richTextBoxLogs.AppendText(users[0].name + " get 1 point\n");
                            sendMessage(users[1].socket, "You lose! You get 0 point\n");
                            richTextBoxLogs.AppendText(users[1].name + " get 0 point\n\n");
                        }
                        else if (result == 1) // if second user won
                        {
                            users[1] = users[1].addPoint(1);
                            sendMessage(users[1].socket, "You win! You get 1 point\n");
                            sendMessage(users[0].socket, "You lose! You get 0 point\n");
                            richTextBoxLogs.AppendText(users[0].name + " get 0 point\n");
                            richTextBoxLogs.AppendText(users[1].name + " get 1 point\n\n");
                        }
                        else // if it is a Tie
                        {
                            users[0] = users[0].addPoint(0.5);
                            users[1] = users[1].addPoint(0.5);
                            sendMessage(users[1].socket, "Tie! You get 0.5 point\n");
                            sendMessage(users[0].socket, "Tie! You get 0.5 point\n");
                            richTextBoxLogs.AppendText(users[0].name + " get 0.5 point\n");
                            richTextBoxLogs.AppendText(users[1].name + " get 0.5 point\n\n");
                        }

                        // print scores to all users and to server log
                        for (int k = 0; k < userCount; k++)
                        {
                            sendMessage(users[k].socket, getScores());
                        }
                        richTextBoxLogs.AppendText(getScores() + "\n");

                        // increment question number and update answered users back to zero
                        incrementCurrentQuestionNumber();
                        updateAnsweredUserCount(-2);
                    }
                }
                bool isFirst = true; // used to check ending message only printed once to server log in case of tie
                // for each user send a message about their winner/loser/tie status and inform that quiz is over
                for (int k = 0; k < users.Count; k++)
                {
                    sendMessage(users[k].socket, "The quiz is over!");
                    sendMessage(users[k].socket, getScores());
                    int winner = getWinner();
                    if(winner == -1) // if game is tie
                    {
                        sendMessage(users[k].socket, "Tie!");
                        // print message only once to server log
                        if (isFirst)
                        {
                            richTextBoxLogs.AppendText("The quiz is over!\n");
                            richTextBoxLogs.AppendText("The result is tie\n");
                            isFirst = false; // set status for message already printed
                        }
                    }
                    else if (k == winner) // if iterated user won, show appropriate message
                    {
                        sendMessage(users[k].socket, "Congratulations! You win!");
                        richTextBoxLogs.AppendText("The quiz is over!\n");
                        richTextBoxLogs.AppendText(users[k].name + " is the winner\n");
                    }
                    else // if iterated user lost, show aproopriate message
                    {
                        sendMessage(users[k].socket, "You lose!");
                    }
                    isFirst = false;
                }
                // update status of if quiz is started as False (set isQuizStarted as FALSE)
                updateOrGetIsQuizStarted(2);
            }
        }
    }
}
