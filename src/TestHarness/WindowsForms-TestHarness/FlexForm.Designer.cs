namespace WinFormsTestHarness
{
    partial class FlexForm
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
            this.LoadStructure = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.EditWithAcme = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.ChangeBackground = new System.Windows.Forms.Button();
            this.ShowCarbons = new System.Windows.Forms.CheckBox();
            this.RemoveAtom = new System.Windows.Forms.Button();
            this.RandomElement = new System.Windows.Forms.Button();
            this.Serialize = new System.Windows.Forms.Button();
            this.Examine = new System.Windows.Forms.Button();
            this.Hex = new System.Windows.Forms.Button();
            this.Timing = new System.Windows.Forms.Button();
            this.Undo = new System.Windows.Forms.Button();
            this.Redo = new System.Windows.Forms.Button();
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.DisplayHost = new System.Windows.Forms.Integration.ElementHost();
            this.Display = new Chem4Word.ACME.Display();
            this.RedoHost = new System.Windows.Forms.Integration.ElementHost();
            this.RedoStack = new WinFormsTestHarness.StackViewer();
            this.UndoHost = new System.Windows.Forms.Integration.ElementHost();
            this.UndoStack = new WinFormsTestHarness.StackViewer();
            this.Information = new System.Windows.Forms.Label();
            this.EditCml = new System.Windows.Forms.Button();
            this.LayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoadStructure
            // 
            this.LoadStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadStructure.Location = new System.Drawing.Point(12, 486);
            this.LoadStructure.Name = "LoadStructure";
            this.LoadStructure.Size = new System.Drawing.Size(75, 23);
            this.LoadStructure.TabIndex = 0;
            this.LoadStructure.Text = "Load";
            this.LoadStructure.UseVisualStyleBackColor = true;
            this.LoadStructure.Click += new System.EventHandler(this.LoadStructure_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // EditWithAcme
            // 
            this.EditWithAcme.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EditWithAcme.Enabled = false;
            this.EditWithAcme.Location = new System.Drawing.Point(1052, 486);
            this.EditWithAcme.Name = "EditWithAcme";
            this.EditWithAcme.Size = new System.Drawing.Size(75, 23);
            this.EditWithAcme.TabIndex = 2;
            this.EditWithAcme.Text = "ACME";
            this.EditWithAcme.UseVisualStyleBackColor = true;
            this.EditWithAcme.Click += new System.EventHandler(this.EditWithAcme_Click);
            // 
            // ChangeBackground
            // 
            this.ChangeBackground.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ChangeBackground.Location = new System.Drawing.Point(93, 486);
            this.ChangeBackground.Name = "ChangeBackground";
            this.ChangeBackground.Size = new System.Drawing.Size(75, 23);
            this.ChangeBackground.TabIndex = 3;
            this.ChangeBackground.Text = "Background";
            this.ChangeBackground.UseVisualStyleBackColor = true;
            this.ChangeBackground.Click += new System.EventHandler(this.ChangeBackground_Click);
            // 
            // ShowCarbons
            // 
            this.ShowCarbons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ShowCarbons.AutoSize = true;
            this.ShowCarbons.Enabled = false;
            this.ShowCarbons.Location = new System.Drawing.Point(465, 490);
            this.ShowCarbons.Name = "ShowCarbons";
            this.ShowCarbons.Size = new System.Drawing.Size(95, 17);
            this.ShowCarbons.TabIndex = 4;
            this.ShowCarbons.Text = "Show Carbons";
            this.ShowCarbons.UseVisualStyleBackColor = true;
            this.ShowCarbons.Visible = false;
            this.ShowCarbons.CheckedChanged += new System.EventHandler(this.ShowCarbons_CheckedChanged);
            // 
            // RemoveAtom
            // 
            this.RemoveAtom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveAtom.Enabled = false;
            this.RemoveAtom.Location = new System.Drawing.Point(940, 486);
            this.RemoveAtom.Name = "RemoveAtom";
            this.RemoveAtom.Size = new System.Drawing.Size(97, 23);
            this.RemoveAtom.TabIndex = 5;
            this.RemoveAtom.Text = "Remove Atom";
            this.RemoveAtom.UseVisualStyleBackColor = true;
            this.RemoveAtom.Click += new System.EventHandler(this.RemoveAtom_Click);
            // 
            // RandomElement
            // 
            this.RandomElement.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RandomElement.Enabled = false;
            this.RandomElement.Location = new System.Drawing.Point(940, 517);
            this.RandomElement.Name = "RandomElement";
            this.RandomElement.Size = new System.Drawing.Size(97, 23);
            this.RandomElement.TabIndex = 6;
            this.RandomElement.Text = "Random Element";
            this.RandomElement.UseVisualStyleBackColor = true;
            this.RandomElement.Click += new System.EventHandler(this.RandomElement_Click);
            // 
            // Serialize
            // 
            this.Serialize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Serialize.Location = new System.Drawing.Point(465, 513);
            this.Serialize.Name = "Serialize";
            this.Serialize.Size = new System.Drawing.Size(55, 23);
            this.Serialize.TabIndex = 7;
            this.Serialize.Text = "Serialize";
            this.Serialize.UseVisualStyleBackColor = true;
            this.Serialize.Visible = false;
            this.Serialize.Click += new System.EventHandler(this.Serialize_Click);
            // 
            // Examine
            // 
            this.Examine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Examine.Location = new System.Drawing.Point(526, 513);
            this.Examine.Name = "Examine";
            this.Examine.Size = new System.Drawing.Size(55, 23);
            this.Examine.TabIndex = 8;
            this.Examine.Text = "Analyse";
            this.Examine.UseVisualStyleBackColor = true;
            this.Examine.Visible = false;
            this.Examine.Click += new System.EventHandler(this.Examine_Click);
            // 
            // Hex
            // 
            this.Hex.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Hex.Location = new System.Drawing.Point(587, 513);
            this.Hex.Name = "Hex";
            this.Hex.Size = new System.Drawing.Size(55, 23);
            this.Hex.TabIndex = 9;
            this.Hex.Text = "Hex";
            this.Hex.UseVisualStyleBackColor = true;
            this.Hex.Visible = false;
            this.Hex.Click += new System.EventHandler(this.Hex_Click);
            // 
            // Timing
            // 
            this.Timing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Timing.Location = new System.Drawing.Point(648, 513);
            this.Timing.Name = "Timing";
            this.Timing.Size = new System.Drawing.Size(55, 23);
            this.Timing.TabIndex = 10;
            this.Timing.Text = "Timing";
            this.Timing.UseVisualStyleBackColor = true;
            this.Timing.Visible = false;
            this.Timing.Click += new System.EventHandler(this.Timing_Click);
            // 
            // Undo
            // 
            this.Undo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Undo.Enabled = false;
            this.Undo.Location = new System.Drawing.Point(12, 519);
            this.Undo.Name = "Undo";
            this.Undo.Size = new System.Drawing.Size(75, 23);
            this.Undo.TabIndex = 11;
            this.Undo.Text = "Undo";
            this.Undo.UseVisualStyleBackColor = true;
            this.Undo.Click += new System.EventHandler(this.Undo_Click);
            // 
            // Redo
            // 
            this.Redo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Redo.Enabled = false;
            this.Redo.Location = new System.Drawing.Point(93, 519);
            this.Redo.Name = "Redo";
            this.Redo.Size = new System.Drawing.Size(75, 23);
            this.Redo.TabIndex = 12;
            this.Redo.Text = "Redo";
            this.Redo.UseVisualStyleBackColor = true;
            this.Redo.Click += new System.EventHandler(this.Redo_Click);
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LayoutPanel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.LayoutPanel.ColumnCount = 3;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 275F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 275F));
            this.LayoutPanel.Controls.Add(this.DisplayHost, 1, 0);
            this.LayoutPanel.Controls.Add(this.RedoHost, 2, 0);
            this.LayoutPanel.Controls.Add(this.UndoHost, 0, 0);
            this.LayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.RowCount = 1;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.Size = new System.Drawing.Size(1115, 435);
            this.LayoutPanel.TabIndex = 13;
            // 
            // DisplayHost
            // 
            this.DisplayHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.DisplayHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayHost.Location = new System.Drawing.Point(278, 3);
            this.DisplayHost.Name = "DisplayHost";
            this.DisplayHost.Size = new System.Drawing.Size(559, 429);
            this.DisplayHost.TabIndex = 1;
            this.DisplayHost.Text = "centreHost";
            this.DisplayHost.Child = this.Display;
            // 
            // RedoHost
            // 
            this.RedoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RedoHost.Location = new System.Drawing.Point(843, 3);
            this.RedoHost.Name = "RedoHost";
            this.RedoHost.Size = new System.Drawing.Size(269, 429);
            this.RedoHost.TabIndex = 2;
            this.RedoHost.Text = "rightHost";
            this.RedoHost.Child = this.RedoStack;
            // 
            // UndoHost
            // 
            this.UndoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UndoHost.Location = new System.Drawing.Point(3, 3);
            this.UndoHost.Name = "UndoHost";
            this.UndoHost.Size = new System.Drawing.Size(269, 429);
            this.UndoHost.TabIndex = 3;
            this.UndoHost.Text = "leftHost";
            this.UndoHost.Child = this.UndoStack;
            // 
            // Information
            // 
            this.Information.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Information.AutoSize = true;
            this.Information.Location = new System.Drawing.Point(12, 460);
            this.Information.Name = "Information";
            this.Information.Size = new System.Drawing.Size(16, 13);
            this.Information.TabIndex = 14;
            this.Information.Text = "...";
            // 
            // EditCml
            // 
            this.EditCml.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EditCml.Enabled = false;
            this.EditCml.Location = new System.Drawing.Point(1052, 517);
            this.EditCml.Name = "EditCml";
            this.EditCml.Size = new System.Drawing.Size(75, 23);
            this.EditCml.TabIndex = 15;
            this.EditCml.Text = "CML";
            this.EditCml.UseVisualStyleBackColor = true;
            this.EditCml.Click += new System.EventHandler(this.EditCml_Click);
            // 
            // FlexForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1139, 544);
            this.Controls.Add(this.EditCml);
            this.Controls.Add(this.Information);
            this.Controls.Add(this.LayoutPanel);
            this.Controls.Add(this.Redo);
            this.Controls.Add(this.Undo);
            this.Controls.Add(this.Timing);
            this.Controls.Add(this.Hex);
            this.Controls.Add(this.Examine);
            this.Controls.Add(this.Serialize);
            this.Controls.Add(this.ShowCarbons);
            this.Controls.Add(this.ChangeBackground);
            this.Controls.Add(this.EditWithAcme);
            this.Controls.Add(this.RandomElement);
            this.Controls.Add(this.RemoveAtom);
            this.Controls.Add(this.LoadStructure);
            this.Name = "FlexForm";
            this.Text = "Flexible Display";
            this.Load += new System.EventHandler(this.FlexForm_Load);
            this.LayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LoadStructure;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Integration.ElementHost DisplayHost;
        private System.Windows.Forms.Button EditWithAcme;
        private Chem4Word.ACME.Display Display;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button ChangeBackground;
        private System.Windows.Forms.CheckBox ShowCarbons;
        private System.Windows.Forms.Button RemoveAtom;
        private System.Windows.Forms.Button RandomElement;
        private System.Windows.Forms.Button Serialize;
        private System.Windows.Forms.Button Examine;
        private System.Windows.Forms.Button Hex;
        private System.Windows.Forms.Button Timing;
        private System.Windows.Forms.Button Undo;
        private System.Windows.Forms.Button Redo;
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.Integration.ElementHost RedoHost;
        private System.Windows.Forms.Integration.ElementHost UndoHost;
        private System.Windows.Forms.Label Information;
        private StackViewer RedoStack;
        private StackViewer UndoStack;
        private System.Windows.Forms.Button EditCml;
    }
}

