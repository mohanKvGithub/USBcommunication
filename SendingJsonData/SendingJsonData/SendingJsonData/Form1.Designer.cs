namespace SendingJsonData
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            textBox1 = new TextBox();
            button2 = new Button();
            textBox2 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(3, 31);
            button1.Name = "button1";
            button1.Size = new Size(105, 23);
            button1.TabIndex = 0;
            button1.Text = "Sync";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(114, 31);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(600, 600);
            textBox1.TabIndex = 1;
            // 
            // button2
            // 
            button2.Location = new Point(3, 72);
            button2.Name = "button2";
            button2.Size = new Size(105, 23);
            button2.TabIndex = 2;
            button2.Text = "Clear Screen";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(731, 32);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.ScrollBars = ScrollBars.Vertical;
            textBox2.Size = new Size(600, 600);
            textBox2.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(798, 12);
            label1.Name = "label1";
            label1.Size = new Size(271, 15);
            label1.TabIndex = 4;
            label1.Text = "READY_NEW_WP_SYNC (Sending Data to android)";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(249, 9);
            label2.Name = "label2";
            label2.Size = new Size(269, 15);
            label2.TabIndex = 5;
            label2.Text = "UPDATE_WP_SYNC (Recieving Data from android)";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1118, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox2);
            Controls.Add(button2);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox textBox1;
        private Button button2;
        private TextBox textBox2;
        private Label label1;
        private Label label2;
    }
}
