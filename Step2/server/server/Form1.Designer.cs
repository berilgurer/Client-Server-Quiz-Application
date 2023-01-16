namespace server
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox();
            this.buttonListen = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxNumberOfQuestions = new System.Windows.Forms.TextBox();
            this.buttonStartGame = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(175, 79);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(123, 22);
            this.textBoxPort.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 79);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Port:";
            // 
            // richTextBoxLogs
            // 
            this.richTextBoxLogs.Location = new System.Drawing.Point(304, 12);
            this.richTextBoxLogs.Name = "richTextBoxLogs";
            this.richTextBoxLogs.Size = new System.Drawing.Size(352, 493);
            this.richTextBoxLogs.TabIndex = 2;
            this.richTextBoxLogs.Text = "";
            // 
            // buttonListen
            // 
            this.buttonListen.Location = new System.Drawing.Point(175, 118);
            this.buttonListen.Name = "buttonListen";
            this.buttonListen.Size = new System.Drawing.Size(123, 23);
            this.buttonListen.TabIndex = 3;
            this.buttonListen.Text = "Listen";
            this.buttonListen.UseVisualStyleBackColor = true;
            this.buttonListen.Click += new System.EventHandler(this.buttonListen_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 165);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(146, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Number of Questions:";
            // 
            // textBoxNumberOfQuestions
            // 
            this.textBoxNumberOfQuestions.Enabled = false;
            this.textBoxNumberOfQuestions.Location = new System.Drawing.Point(175, 162);
            this.textBoxNumberOfQuestions.Name = "textBoxNumberOfQuestions";
            this.textBoxNumberOfQuestions.Size = new System.Drawing.Size(123, 22);
            this.textBoxNumberOfQuestions.TabIndex = 5;
            // 
            // buttonStartGame
            // 
            this.buttonStartGame.Enabled = false;
            this.buttonStartGame.Location = new System.Drawing.Point(175, 197);
            this.buttonStartGame.Name = "buttonStartGame";
            this.buttonStartGame.Size = new System.Drawing.Size(123, 23);
            this.buttonStartGame.TabIndex = 6;
            this.buttonStartGame.Text = "Start Game";
            this.buttonStartGame.UseVisualStyleBackColor = true;
            this.buttonStartGame.Click += new System.EventHandler(this.buttonStartGame_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(667, 517);
            this.Controls.Add(this.buttonStartGame);
            this.Controls.Add(this.textBoxNumberOfQuestions);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonListen);
            this.Controls.Add(this.richTextBoxLogs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxPort);
            this.Name = "Form1";
            this.Text = "Server";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBoxLogs;
        private System.Windows.Forms.Button buttonListen;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxNumberOfQuestions;
        private System.Windows.Forms.Button buttonStartGame;
    }
}

