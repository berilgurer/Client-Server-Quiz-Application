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
        List<player> waitingList = new List<player>();
        List<question> questions = new List<question>();
        int[] answers;
        List<string> removed = new List<string>();

        int numOfQuestions = 0;
        bool terminating = false;
        bool listening = false;
        bool isEnoughUser = false;
        int answeredUserCount = 0;
        int currentQuestionNumber = 0;
        int currentUserNumber = 0;
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
                textBoxNumberOfQuestions.Enabled = true;
                buttonListen.Enabled = false;


                Thread acceptThread = new Thread(Accept);
                acceptThread.IsBackground = true;
                acceptThread.Start();

                richTextBoxLogs.AppendText("Started listening on port " + serverPort + "\n");
            }
            else // appropriate error message for port number
            {
                richTextBoxLogs.AppendText("Please check port number \n");
            }
        }

        private void printUsers()
        {
            string a = "Users: ";
            for (int i = 0; i < users.Count; i++)
            {
                a += users[i].id + "-" + users[i].name + " ";
            }
            a += "Waiting List: ";
            for (int i = 0; i < waitingList.Count; i++)
            {
                a += waitingList[i].id + "-" + waitingList[i].name + " ";
            }
            a += "\n";
            a += "Answers: ";
            for (int i = 0; i < answers.Length; i++)
            {
                a += answers[i].ToString()+ " ";
            }
            a += "\n";
            richTextBoxLogs.AppendText(a);
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

            for (int i = 0; i < waitingList.Count; i++)
            {
                if (newName == waitingList[i].name)
                {
                    return true;
                }
            }
            return false;
        }

        private bool isInUsers(int id)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (id == users[i].id)
                {
                    return true;
                }
            }
            return false;
        }

        private int indexFromId(int id)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (id == users[i].id)
                {
                    return i;
                }
            }
            return -1;
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
            int[] winnerList = getWinner();
            richTextBoxLogs.AppendText("The quiz is over!\n");
            // for each user send a message about their winner/loser/tie status and inform that quiz is over
            for (int k = 0; k < users.Count; k++)
            {
                sendMessage(users[k].socket, "The quiz is over!");
                sendMessage(users[k].socket, getScores());
            }

            for (int j = 0; j < users.Count; j++)
            {
                if (winnerList[j] == 2)
                {
                    sendMessage(users[j].socket, "Congratulations! You are the only winner!");
                    richTextBoxLogs.AppendText(users[j].name + " is the only winner\n");
                }

                else if (winnerList[j] == 1)
                {
                    sendMessage(users[j].socket, "Congratulations! You are one of the winners!");
                    richTextBoxLogs.AppendText(users[j].name + " is one of the winners\n");
                }
                else
                {
                    sendMessage(users[j].socket, "You lose!");
                }
            }

            // update status of if quiz is started as False (set isQuizStarted as FALSE)
            updateOrGetIsQuizStarted(2);
            //printUsers();
            numOfQuestions = 0;
            terminating = false;
            //listening = false;
            //isEnoughUser = false;
            answeredUserCount = 0;
            currentQuestionNumber = 0;
            currentUserNumber = 0;
            updateOrGetIsQuizStarted(2);
            disconnectedCount = 0;
            buttonStartGame.Enabled = true;
            removed.Clear();
            for (int i = 0; i < users.Count; i++)
            {
                users[i] = users[i].zeroPoint();
            }


            for (int i = 0; i<waitingList.Count; i++)
            {
                player temp = waitingList[i];
                users.Add(temp);
            }

            waitingList.Clear();

            if (users.Count < 2)
            {
                isEnoughUser = false;
                buttonStartGame.Enabled = false;
            }
            //printUsers();
        }

        // accept players trying to connect
        private void Accept()
        {
            while (listening)
            {
                try
                {
                    while(!updateOrGetIsQuizStarted(1)) // while the quiz is not started
                    {
                        Socket newClient = serverSocket.Accept();
                        Byte[] buffer = new Byte[1024];
                        newClient.Receive(buffer);
                        string nameOfClient = Encoding.Default.GetString(buffer);
                        nameOfClient = nameOfClient.Substring(0, nameOfClient.IndexOf("\0"));
                        
                        if (!isNameExists(nameOfClient) && !updateOrGetIsQuizStarted(1)) //check name of user is unique
                        {
                            //Create new player and add to the users list
                            player newUser = new player(idCount, nameOfClient, newClient);
                            users.Add(newUser);
                            idCount++;
                            sendMessage(newClient, "Connection Success!");
                            Thread receiveThread = new Thread(() => Receive(ref newUser));
                            receiveThread.IsBackground = true;
                            receiveThread.Start();
                            //log connected user
                            richTextBoxLogs.AppendText(nameOfClient + " is connected.\n");
                            // condition update
                            if (users.Count == 2)
                            {
                                isEnoughUser = true;
                                buttonStartGame.Enabled = true;
                            }
                        }
                        else if (!isNameExists(nameOfClient) && updateOrGetIsQuizStarted(1)) //check name of user is unique
                        {
                            player newUser = new player(idCount, nameOfClient, newClient);
                            idCount++;
                            waitingList.Add(newUser);
                            sendMessage(newUser.socket, "There is a running quiz right now! Please wait");
                            richTextBoxLogs.AppendText(nameOfClient + " is connected and put to the waiting list.\n");
                            Thread receiveThread = new Thread(() => Receive(ref newUser));
                            receiveThread.IsBackground = true;
                            receiveThread.Start();
                        }

                        else // if player trying to connect doesn't have a unique name show error
                        {
                            richTextBoxLogs.AppendText(nameOfClient + " tries to connect but this name already connected.\n");
                            sendMessage(newClient, "Connection Failed! This name is already in use. Try with another name.");
                            newClient.Close();
                        }
                    }

                    // --------------------------------------------------------------------------------//
                    Socket newExternalClient = serverSocket.Accept();
                    Byte[] bufferExternal = new Byte[1024];
                    newExternalClient.Receive(bufferExternal);
                    string nameOfClientExternal = Encoding.Default.GetString(bufferExternal);
                    nameOfClientExternal = nameOfClientExternal.Substring(0, nameOfClientExternal.IndexOf("\0"));
                    if (!isNameExists(nameOfClientExternal) && !updateOrGetIsQuizStarted(1)) //check name of user is unique
                    {
                        //Create new player and add to the users list
                        player newUser = new player(idCount, nameOfClientExternal, newExternalClient);
                        users.Add(newUser);
                        idCount++;
                        sendMessage(newExternalClient, "Connection Success!");
                        Thread receiveThread = new Thread(() => Receive(ref newUser));
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                        //log connected user
                        richTextBoxLogs.AppendText(nameOfClientExternal + " is connected.\n");
                        // condition update
                        if (users.Count == 2)
                        {
                            isEnoughUser = true;
                            buttonStartGame.Enabled = true;
                        }
                    }
                    else if (!isNameExists(nameOfClientExternal) && updateOrGetIsQuizStarted(1)) //check name of user is unique
                    {
                        player newUser = new player(idCount, nameOfClientExternal, newExternalClient);
                        idCount++;
                        waitingList.Add(newUser);
                        sendMessage(newUser.socket, "There is a running quiz right now! Please wait");
                        richTextBoxLogs.AppendText(nameOfClientExternal + " is connected and put to the waiting list.\n");
                        Thread receiveThread = new Thread(() => Receive(ref newUser));
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                    }

                    else // if player trying to connect doesn't have a unique name show error
                    {
                        richTextBoxLogs.AppendText(nameOfClientExternal + " tries to connect but this name already connected.\n");
                        sendMessage(newExternalClient, "Connection Failed! This name is already in use. Try with another name.");
                        newExternalClient.Close();
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
                    Byte[] buffer = new Byte[1024];
                    newClient.Receive(buffer);
                    string nameOfClient = Encoding.Default.GetString(buffer);
                    nameOfClient = nameOfClient.Substring(0, nameOfClient.IndexOf("\0"));
      
                    if (!isNameExists(nameOfClient)) //check name of user is unique
                    {
                        //Create new player and add to the users list
                        player newUser = new player(idCount, nameOfClient, newClient);
                        waitingList.Add(newUser);
                        idCount++;
                        sendMessage(newUser.socket, "There is a running quiz right now! Please wait");
                        richTextBoxLogs.AppendText(nameOfClient + " is connected and put to the waiting list.\n");

                    }
                    else // if player trying to connect doesn't have a unique name show error
                    {
                        richTextBoxLogs.AppendText(nameOfClient + " tries to connect but this name already connected.\n");
                        sendMessage(newClient, "Connection Failed! This name is already in use. Try with another name.");
                        newClient.Close();
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        richTextBoxLogs.AppendText("The socket stopped working ali.\n");
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
                    richTextBoxLogs.AppendText("There is a problem! Check the connection...\n");
                    terminating = true;
                    textBoxPort.Enabled = true;
                    buttonListen.Enabled = true;
                    serverSocket.Close();
                }
            }
            Thread.Sleep(100);
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
        private void Receive(ref player player)
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
                    if (isInUsers(player.id))
                    {
                        users.RemoveAt(indexFromId(player.id));
                        if (updateOrGetIsQuizStarted(1))
                        {
                            removed.Add(player.name);
                        }
                    }
                    else
                    {
                        //waitingList.Remove(player);
                        waitingList.RemoveAt(indexFromId(player.id));
                    }

                    if (updateOrGetIsQuizStarted(1))
                    {
                        answers[player.id] = Int32.MaxValue;
                    }


                    if (users.Count < 2)
                    {
                        isEnoughUser = false;
                        buttonStartGame.Enabled = false;
                    }

                    connected = false;
                    Thread.Sleep(100);

                    if (updateOrGetIsQuizStarted(1) && users.Count == 1) // if quiz is started and 2 players did not disconnected beacuse of termination of program (some player disconnected ingame)
                    {
                        sendMessage(users[0].socket, "The other players are disconnected. You win the game!");
                        //sendMessage(users[0].socket, getScores()); // send each user the score table
                        //richTextBoxLogs.AppendText("before terminate quiz \n");
                        terminateQuiz(); // finish the quiz appropriately
                        //richTextBoxLogs.AppendText("after terminate quiz \n");
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

            foreach (string name in removed)
            {
                result += name + " - 0\n";
            }

            return result;
        }

        // find and return who is the winner users index, return -1 if game is tie
        private int[] getWinner()
        {
            int[] result = new int[users.Count];
            double maxPoint = users[0].points;
            int maxCount = 0;
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].points > maxPoint)
                {
                    maxPoint = users[i].points;
                }
            }

            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].points == maxPoint)
                {
                    maxCount++;
                }
            }

            for (int k = 0; k < users.Count; k++)
            {
                if (users[k].points == maxPoint && maxCount == 1)
                {
                    result[k] = 2; //adam kazandı
                }
                else if (users[k].points == maxPoint && maxCount > 1)
                {
                    result[k] = 1; //adamlar kazandı
                }
                else
                {
                    result[k] = 0; //basaramadik abi
                }
            }

            return result;
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
        private double[] compareResult(int[] answers, int actualResult)
        {
            int[] distance = new int[answers.Length];
            double[] points = new double[answers.Length];
            int minDist = Int32.MaxValue;
            int minCount = 0;
            for(int i = 0; i < answers.Length; i++)
            {
                int d = Math.Abs(answers[i] - actualResult);
                distance[i] = d;
                if(d < minDist)
                {
                    minDist = d;
                }
            }

            for (int i = 0; i < answers.Length; i++)
            {
                if (distance[i] == minDist)
                {
                    minCount++;
                }
            }

            for (int k = 0; k < answers.Length; k++)
            {
                if(distance[k] == minDist && minCount == 1)
                {
                    points[k] = 1.0;
                }
                else if (distance[k] == minDist && minCount != 1)
                {
                    points[k] = 1.0/ minCount;
                }
                else
                {
                    points[k] = 0.0;
                }
            }
            return points;
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
        // increment the current quesitons number by one
        private void incrementCurrentUserNumber()
        {
            lock (this)
            {
                currentUserNumber += 1;
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
                    for (int k = 0; k < users.Count; k++) 
                    {
                        sendQuestion(questions[i % questions.Count].questionString, users[k]);
                    }

                    // print question the the server log
                    richTextBoxLogs.AppendText("Question - "+ (i+1).ToString() +" "+ questions[i % questions.Count].questionString + "\n\n");

                    // Wait if all users did not answer
                    while (answeredUserCount != users.Count && updateOrGetIsQuizStarted(1))
                    {
                        ;
                    }
                    Thread.Sleep(1000);
                    //richTextBoxLogs.AppendText("while loopu geçtim hocam");
                    // if all users answered
                    if (updateOrGetIsQuizStarted(1) && answeredUserCount == users.Count)
                    {
                        string answersString = "";
                        for (int a = 0; a < users.Count; a++)
                        {
                            answersString += users[a].name + "\'s answer is " + answers[users[a].id].ToString() + "\n";
                        }
                        answersString += "The correct answer was " + questions[i % questions.Count].answer.ToString() + "\n";
                        // send all users answers and the correct answer to the each user and server log
                        for (int k = 0; k < users.Count; k++)
                        {
                            sendMessage(users[k].socket, answersString);
                        }

                        richTextBoxLogs.AppendText(answersString + "\n");

                        // decide who win that round, distribute appropriate points to users with appropriate message
                        double[] result = compareResult(answers, questions[i % questions.Count].answer);
                        for (int b = 0; b < users.Count; b++)
                        {
                            int id = users[b].id;
                            users[b] = users[b].addPoint(result[id]);
                            if (result[id] == 1.0)
                            {
                                sendMessage(users[b].socket, "You win! You get 1 point\n");
                                richTextBoxLogs.AppendText(users[b].name + " get 1 point\n");
                            }
                            else if (result[id] == 0.0)
                            {
                                sendMessage(users[b].socket, "You lose! You get 0 point\n");
                                richTextBoxLogs.AppendText(users[b].name + " get 0 point\n");
                            }
                            else
                            {
                                sendMessage(users[b].socket, "Tie! You get "+ result[id].ToString() +" point\n");
                                richTextBoxLogs.AppendText(users[b].name + " get "+ result[id].ToString() + " point\n");
                            }
                            
                        }

                        // print scores to all users and to server log
                        for (int k = 0; k < users.Count; k++)
                        {
                            sendMessage(users[k].socket, getScores());
                        }
                        richTextBoxLogs.AppendText(getScores() + "\n");

                        // increment question number and update answered users back to zero
                        incrementCurrentQuestionNumber();
                        updateAnsweredUserCount(-1*users.Count);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void startQuiz()
        {
            if (!updateOrGetIsQuizStarted(1))
            {
                buttonStartGame.Enabled = false;
                answers = new int[idCount+1]; // create answers array
                updateOrGetIsQuizStarted(3); // set isQuizStarted as TRUE
                readQuestionsTxt(); // read the questions from txt
                Quiz(); // start the quiz

                if (updateOrGetIsQuizStarted(1))
                {
                    terminateQuiz();
                }
            }
        }

        private void buttonStartGame_Click(object sender, EventArgs e)
        {
            // convert num of questions to integer
            if (Int32.TryParse(textBoxNumberOfQuestions.Text, out numOfQuestions))
            {
                Thread quizThread = new Thread(startQuiz);
                quizThread.IsBackground = true;
                quizThread.Start();
            }
            else
            {
                richTextBoxLogs.AppendText("Invalid number of questions. Please Check!\n");
            }
        }
    }
}
