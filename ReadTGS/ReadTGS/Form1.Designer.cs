namespace ReadTGS
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
            this.QuitButton = new System.Windows.Forms.Button();
            this.ReadTGSButton = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.testbutton = new System.Windows.Forms.Button();
            this.tbStartfile = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button_nosig = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.vtButton = new System.Windows.Forms.RadioButton();
            this.htButton = new System.Windows.Forms.RadioButton();
            this.yearBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.aclabel = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioIoS = new System.Windows.Forms.RadioButton();
            this.radioUHS = new System.Windows.Forms.RadioButton();
            this.radioHM = new System.Windows.Forms.RadioButton();
            this.courselistbutton = new System.Windows.Forms.Button();
            this.outfilebutton = new System.Windows.Forms.Button();
            this.dbinitButton = new System.Windows.Forms.Button();
            this.db_TGSbutton = new System.Windows.Forms.Button();
            this.proglistbutton = new System.Windows.Forms.Button();
            this.batchentry_button = new System.Windows.Forms.Button();
            this.LakanButton = new System.Windows.Forms.Button();
            this.courseprogram = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // QuitButton
            // 
            this.QuitButton.Location = new System.Drawing.Point(721, 737);
            this.QuitButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.QuitButton.Name = "QuitButton";
            this.QuitButton.Size = new System.Drawing.Size(155, 98);
            this.QuitButton.TabIndex = 0;
            this.QuitButton.Text = "Quit";
            this.QuitButton.UseVisualStyleBackColor = true;
            this.QuitButton.Click += new System.EventHandler(this.QuitButton_Click);
            // 
            // ReadTGSButton
            // 
            this.ReadTGSButton.Location = new System.Drawing.Point(721, 1);
            this.ReadTGSButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ReadTGSButton.Name = "ReadTGSButton";
            this.ReadTGSButton.Size = new System.Drawing.Size(155, 43);
            this.ReadTGSButton.TabIndex = 1;
            this.ReadTGSButton.Text = "Read TGS Excel files";
            this.ReadTGSButton.UseVisualStyleBackColor = true;
            this.ReadTGSButton.Click += new System.EventHandler(this.ReadTGSButton_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(27, 38);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(665, 798);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";
            // 
            // testbutton
            // 
            this.testbutton.Location = new System.Drawing.Point(721, 702);
            this.testbutton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.testbutton.Name = "testbutton";
            this.testbutton.Size = new System.Drawing.Size(155, 30);
            this.testbutton.TabIndex = 3;
            this.testbutton.Text = "test";
            this.testbutton.UseVisualStyleBackColor = true;
            this.testbutton.Click += new System.EventHandler(this.testbutton_Click);
            // 
            // tbStartfile
            // 
            this.tbStartfile.Location = new System.Drawing.Point(709, 201);
            this.tbStartfile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbStartfile.Name = "tbStartfile";
            this.tbStartfile.Size = new System.Drawing.Size(153, 22);
            this.tbStartfile.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(715, 181);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(139, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "Only files containing:";
            // 
            // button_nosig
            // 
            this.button_nosig.Location = new System.Drawing.Point(721, 660);
            this.button_nosig.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_nosig.Name = "button_nosig";
            this.button_nosig.Size = new System.Drawing.Size(155, 36);
            this.button_nosig.TabIndex = 6;
            this.button_nosig.Text = "No signature";
            this.button_nosig.UseVisualStyleBackColor = true;
            this.button_nosig.Click += new System.EventHandler(this.button_nosig_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(709, 230);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 17);
            this.label2.TabIndex = 7;
            this.label2.Text = "Max # files:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(803, 230);
            this.textBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(61, 22);
            this.textBox1.TabIndex = 8;
            this.textBox1.ModifiedChanged += new System.EventHandler(this.textBox1_ModifiedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.vtButton);
            this.groupBox1.Controls.Add(this.htButton);
            this.groupBox1.Location = new System.Drawing.Point(721, 258);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Size = new System.Drawing.Size(115, 47);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Semester";
            // 
            // vtButton
            // 
            this.vtButton.AutoSize = true;
            this.vtButton.Location = new System.Drawing.Point(60, 20);
            this.vtButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.vtButton.Name = "vtButton";
            this.vtButton.Size = new System.Drawing.Size(47, 21);
            this.vtButton.TabIndex = 1;
            this.vtButton.TabStop = true;
            this.vtButton.Text = "VT";
            this.vtButton.UseVisualStyleBackColor = true;
            this.vtButton.CheckedChanged += new System.EventHandler(this.vtButton_CheckedChanged);
            // 
            // htButton
            // 
            this.htButton.AutoSize = true;
            this.htButton.Checked = true;
            this.htButton.Location = new System.Drawing.Point(5, 21);
            this.htButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.htButton.Name = "htButton";
            this.htButton.Size = new System.Drawing.Size(48, 21);
            this.htButton.TabIndex = 0;
            this.htButton.TabStop = true;
            this.htButton.Text = "HT";
            this.htButton.UseVisualStyleBackColor = true;
            this.htButton.CheckedChanged += new System.EventHandler(this.htButton_CheckedChanged);
            // 
            // yearBox
            // 
            this.yearBox.Location = new System.Drawing.Point(795, 319);
            this.yearBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.yearBox.Name = "yearBox";
            this.yearBox.Size = new System.Drawing.Size(81, 22);
            this.yearBox.TabIndex = 11;
            this.yearBox.Text = "2016";
            this.yearBox.ModifiedChanged += new System.EventHandler(this.yearBox_ModifiedChanged);
            this.yearBox.TextChanged += new System.EventHandler(this.yearBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(725, 319);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 17);
            this.label3.TabIndex = 12;
            this.label3.Text = "Year:";
            // 
            // aclabel
            // 
            this.aclabel.AutoSize = true;
            this.aclabel.Location = new System.Drawing.Point(847, 281);
            this.aclabel.Name = "aclabel";
            this.aclabel.Size = new System.Drawing.Size(53, 17);
            this.aclabel.TabIndex = 13;
            this.aclabel.Text = "aclabel";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioIoS);
            this.groupBox2.Controls.Add(this.radioUHS);
            this.groupBox2.Controls.Add(this.radioHM);
            this.groupBox2.Location = new System.Drawing.Point(687, 134);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Size = new System.Drawing.Size(208, 44);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Academy";
            // 
            // radioIoS
            // 
            this.radioIoS.AutoSize = true;
            this.radioIoS.Location = new System.Drawing.Point(128, 17);
            this.radioIoS.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.radioIoS.Name = "radioIoS";
            this.radioIoS.Size = new System.Drawing.Size(49, 21);
            this.radioIoS.TabIndex = 2;
            this.radioIoS.Text = "IoS";
            this.radioIoS.UseVisualStyleBackColor = true;
            this.radioIoS.CheckedChanged += new System.EventHandler(this.radioIoS_CheckedChanged);
            // 
            // radioUHS
            // 
            this.radioUHS.AutoSize = true;
            this.radioUHS.Location = new System.Drawing.Point(63, 17);
            this.radioUHS.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.radioUHS.Name = "radioUHS";
            this.radioUHS.Size = new System.Drawing.Size(58, 21);
            this.radioUHS.TabIndex = 1;
            this.radioUHS.Text = "UHS";
            this.radioUHS.UseVisualStyleBackColor = true;
            this.radioUHS.CheckedChanged += new System.EventHandler(this.radioUHS_CheckedChanged);
            // 
            // radioHM
            // 
            this.radioHM.AutoSize = true;
            this.radioHM.Checked = true;
            this.radioHM.Location = new System.Drawing.Point(7, 17);
            this.radioHM.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.radioHM.Name = "radioHM";
            this.radioHM.Size = new System.Drawing.Size(50, 21);
            this.radioHM.TabIndex = 0;
            this.radioHM.TabStop = true;
            this.radioHM.Text = "HM";
            this.radioHM.UseVisualStyleBackColor = true;
            this.radioHM.CheckedChanged += new System.EventHandler(this.radioHM_CheckedChanged);
            // 
            // courselistbutton
            // 
            this.courselistbutton.Location = new System.Drawing.Point(721, 357);
            this.courselistbutton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.courselistbutton.Name = "courselistbutton";
            this.courselistbutton.Size = new System.Drawing.Size(155, 44);
            this.courselistbutton.TabIndex = 15;
            this.courselistbutton.Text = "Read course lists";
            this.courselistbutton.UseVisualStyleBackColor = true;
            this.courselistbutton.Click += new System.EventHandler(this.courselistbutton_Click);
            // 
            // outfilebutton
            // 
            this.outfilebutton.Location = new System.Drawing.Point(721, 48);
            this.outfilebutton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.outfilebutton.Name = "outfilebutton";
            this.outfilebutton.Size = new System.Drawing.Size(155, 41);
            this.outfilebutton.TabIndex = 16;
            this.outfilebutton.Text = "Read TGS .txt files";
            this.outfilebutton.UseVisualStyleBackColor = true;
            this.outfilebutton.Click += new System.EventHandler(this.outfilebutton_Click);
            // 
            // dbinitButton
            // 
            this.dbinitButton.Location = new System.Drawing.Point(721, 455);
            this.dbinitButton.Margin = new System.Windows.Forms.Padding(4);
            this.dbinitButton.Name = "dbinitButton";
            this.dbinitButton.Size = new System.Drawing.Size(155, 46);
            this.dbinitButton.TabIndex = 17;
            this.dbinitButton.Text = "Database setup";
            this.dbinitButton.UseVisualStyleBackColor = true;
            this.dbinitButton.Click += new System.EventHandler(this.dbinitButton_Click);
            // 
            // db_TGSbutton
            // 
            this.db_TGSbutton.Enabled = false;
            this.db_TGSbutton.Location = new System.Drawing.Point(721, 508);
            this.db_TGSbutton.Name = "db_TGSbutton";
            this.db_TGSbutton.Size = new System.Drawing.Size(155, 44);
            this.db_TGSbutton.TabIndex = 18;
            this.db_TGSbutton.Text = "Upload TGS to database";
            this.db_TGSbutton.UseVisualStyleBackColor = true;
            this.db_TGSbutton.Click += new System.EventHandler(this.db_TGSbutton_Click);
            // 
            // proglistbutton
            // 
            this.proglistbutton.Location = new System.Drawing.Point(721, 406);
            this.proglistbutton.Name = "proglistbutton";
            this.proglistbutton.Size = new System.Drawing.Size(155, 42);
            this.proglistbutton.TabIndex = 19;
            this.proglistbutton.Text = "Read program lists";
            this.proglistbutton.UseVisualStyleBackColor = true;
            this.proglistbutton.Click += new System.EventHandler(this.proglistbutton_Click);
            // 
            // batchentry_button
            // 
            this.batchentry_button.Location = new System.Drawing.Point(721, 558);
            this.batchentry_button.Name = "batchentry_button";
            this.batchentry_button.Size = new System.Drawing.Size(155, 35);
            this.batchentry_button.TabIndex = 20;
            this.batchentry_button.Text = "Make batchentry";
            this.batchentry_button.UseVisualStyleBackColor = true;
            this.batchentry_button.Click += new System.EventHandler(this.batchentry_button_Click);
            // 
            // LakanButton
            // 
            this.LakanButton.Location = new System.Drawing.Point(721, 94);
            this.LakanButton.Name = "LakanButton";
            this.LakanButton.Size = new System.Drawing.Size(155, 35);
            this.LakanButton.TabIndex = 21;
            this.LakanButton.Text = "Read special sheets";
            this.LakanButton.UseVisualStyleBackColor = true;
            this.LakanButton.Click += new System.EventHandler(this.LakanButton_Click);
            // 
            // courseprogram
            // 
            this.courseprogram.Location = new System.Drawing.Point(721, 599);
            this.courseprogram.Name = "courseprogram";
            this.courseprogram.Size = new System.Drawing.Size(159, 39);
            this.courseprogram.TabIndex = 22;
            this.courseprogram.Text = "Read course-program connections";
            this.courseprogram.UseCompatibleTextRendering = true;
            this.courseprogram.UseVisualStyleBackColor = true;
            this.courseprogram.Click += new System.EventHandler(this.courseprogram_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(892, 871);
            this.Controls.Add(this.courseprogram);
            this.Controls.Add(this.LakanButton);
            this.Controls.Add(this.batchentry_button);
            this.Controls.Add(this.proglistbutton);
            this.Controls.Add(this.db_TGSbutton);
            this.Controls.Add(this.dbinitButton);
            this.Controls.Add(this.outfilebutton);
            this.Controls.Add(this.courselistbutton);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.aclabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.yearBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button_nosig);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbStartfile);
            this.Controls.Add(this.testbutton);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.ReadTGSButton);
            this.Controls.Add(this.QuitButton);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button QuitButton;
        private System.Windows.Forms.Button ReadTGSButton;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button testbutton;
        private System.Windows.Forms.TextBox tbStartfile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_nosig;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton vtButton;
        private System.Windows.Forms.RadioButton htButton;
        private System.Windows.Forms.TextBox yearBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label aclabel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioIoS;
        private System.Windows.Forms.RadioButton radioUHS;
        private System.Windows.Forms.RadioButton radioHM;
        private System.Windows.Forms.Button courselistbutton;
        private System.Windows.Forms.Button outfilebutton;
        private System.Windows.Forms.Button dbinitButton;
        private System.Windows.Forms.Button db_TGSbutton;
        private System.Windows.Forms.Button proglistbutton;
        private System.Windows.Forms.Button batchentry_button;
        private System.Windows.Forms.Button LakanButton;
        private System.Windows.Forms.Button courseprogram;
    }
}

