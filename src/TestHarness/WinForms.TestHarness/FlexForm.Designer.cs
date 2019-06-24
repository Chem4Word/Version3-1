using WinFormsTestHarness;

namespace WinForms.TestHarness
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FlexForm));
            this.LoadStructure = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.EditWithAcme = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.ChangeBackground = new System.Windows.Forms.Button();
            this.ShowCarbons = new System.Windows.Forms.CheckBox();
            this.RemoveAtom = new System.Windows.Forms.Button();
            this.RandomElement = new System.Windows.Forms.Button();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ShowCml = new System.Windows.Forms.Button();
            this.SaveStructure = new System.Windows.Forms.Button();
            this.LayoutPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoadStructure
            // 
            this.LoadStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadStructure.Location = new System.Drawing.Point(12, 504);
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
            this.EditWithAcme.Enabled = false;
            this.EditWithAcme.Location = new System.Drawing.Point(6, 19);
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
            this.ChangeBackground.Location = new System.Drawing.Point(209, 533);
            this.ChangeBackground.Name = "ChangeBackground";
            this.ChangeBackground.Size = new System.Drawing.Size(75, 23);
            this.ChangeBackground.TabIndex = 3;
            this.ChangeBackground.Text = "Background";
            this.ChangeBackground.UseVisualStyleBackColor = true;
            this.ChangeBackground.Visible = false;
            this.ChangeBackground.Click += new System.EventHandler(this.ChangeBackground_Click);
            // 
            // ShowCarbons
            // 
            this.ShowCarbons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ShowCarbons.AutoSize = true;
            this.ShowCarbons.Enabled = false;
            this.ShowCarbons.Location = new System.Drawing.Point(523, 501);
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
            this.RemoveAtom.Enabled = false;
            this.RemoveAtom.Location = new System.Drawing.Point(6, 19);
            this.RemoveAtom.Name = "RemoveAtom";
            this.RemoveAtom.Size = new System.Drawing.Size(97, 23);
            this.RemoveAtom.TabIndex = 5;
            this.RemoveAtom.Text = "Remove Atom";
            this.RemoveAtom.UseVisualStyleBackColor = true;
            this.RemoveAtom.Click += new System.EventHandler(this.RemoveAtom_Click);
            // 
            // RandomElement
            // 
            this.RandomElement.Enabled = false;
            this.RandomElement.Location = new System.Drawing.Point(6, 50);
            this.RandomElement.Name = "RandomElement";
            this.RandomElement.Size = new System.Drawing.Size(97, 23);
            this.RandomElement.TabIndex = 6;
            this.RandomElement.Text = "Random Element";
            this.RandomElement.UseVisualStyleBackColor = true;
            this.RandomElement.Click += new System.EventHandler(this.RandomElement_Click);
            // 
            // Undo
            // 
            this.Undo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Undo.Enabled = false;
            this.Undo.Location = new System.Drawing.Point(12, 533);
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
            this.Redo.Location = new System.Drawing.Point(93, 533);
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
            this.LayoutPanel.Size = new System.Drawing.Size(1113, 453);
            this.LayoutPanel.TabIndex = 13;
            // 
            // DisplayHost
            // 
            this.DisplayHost.BackColor = System.Drawing.Color.White;
            this.DisplayHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayHost.Location = new System.Drawing.Point(278, 3);
            this.DisplayHost.Name = "DisplayHost";
            this.DisplayHost.Size = new System.Drawing.Size(557, 447);
            this.DisplayHost.TabIndex = 1;
            this.DisplayHost.Text = "centreHost";
            this.DisplayHost.Child = this.Display;
            // 
            // RedoHost
            // 
            this.RedoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RedoHost.Location = new System.Drawing.Point(841, 3);
            this.RedoHost.Name = "RedoHost";
            this.RedoHost.Size = new System.Drawing.Size(269, 447);
            this.RedoHost.TabIndex = 2;
            this.RedoHost.Text = "rightHost";
            this.RedoHost.Child = this.RedoStack;
            // 
            // UndoHost
            // 
            this.UndoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UndoHost.Location = new System.Drawing.Point(3, 3);
            this.UndoHost.Name = "UndoHost";
            this.UndoHost.Size = new System.Drawing.Size(269, 447);
            this.UndoHost.TabIndex = 3;
            this.UndoHost.Text = "leftHost";
            this.UndoHost.Child = this.UndoStack;
            // 
            // Information
            // 
            this.Information.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Information.AutoSize = true;
            this.Information.Location = new System.Drawing.Point(12, 478);
            this.Information.Name = "Information";
            this.Information.Size = new System.Drawing.Size(16, 13);
            this.Information.TabIndex = 14;
            this.Information.Text = "...";
            // 
            // EditCml
            // 
            this.EditCml.Enabled = false;
            this.EditCml.Location = new System.Drawing.Point(6, 50);
            this.EditCml.Name = "EditCml";
            this.EditCml.Size = new System.Drawing.Size(75, 23);
            this.EditCml.TabIndex = 15;
            this.EditCml.Text = "CML";
            this.EditCml.UseVisualStyleBackColor = true;
            this.EditCml.Click += new System.EventHandler(this.EditCml_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.EditWithAcme);
            this.groupBox1.Controls.Add(this.EditCml);
            this.groupBox1.Location = new System.Drawing.Point(1036, 478);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(89, 80);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Edit with";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.RemoveAtom);
            this.groupBox2.Controls.Add(this.RandomElement);
            this.groupBox2.Location = new System.Drawing.Point(911, 478);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(119, 82);
            this.groupBox2.TabIndex = 17;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Experiments";
            // 
            // ShowCml
            // 
            this.ShowCml.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ShowCml.Enabled = false;
            this.ShowCml.Location = new System.Drawing.Point(532, 528);
            this.ShowCml.Name = "ShowCml";
            this.ShowCml.Size = new System.Drawing.Size(75, 23);
            this.ShowCml.TabIndex = 18;
            this.ShowCml.Text = "Show CML";
            this.ShowCml.UseVisualStyleBackColor = true;
            this.ShowCml.Click += new System.EventHandler(this.ShowCml_Click);
            // 
            // SaveStructure
            // 
            this.SaveStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SaveStructure.Location = new System.Drawing.Point(93, 504);
            this.SaveStructure.Name = "SaveStructure";
            this.SaveStructure.Size = new System.Drawing.Size(75, 23);
            this.SaveStructure.TabIndex = 19;
            this.SaveStructure.Text = "Save ...";
            this.SaveStructure.UseVisualStyleBackColor = true;
            this.SaveStructure.Click += new System.EventHandler(this.SaveStructure_Click);
            // 
            // FlexForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1137, 562);
            this.Controls.Add(this.SaveStructure);
            this.Controls.Add(this.ShowCml);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Information);
            this.Controls.Add(this.LayoutPanel);
            this.Controls.Add(this.Redo);
            this.Controls.Add(this.Undo);
            this.Controls.Add(this.ShowCarbons);
            this.Controls.Add(this.ChangeBackground);
            this.Controls.Add(this.LoadStructure);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FlexForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Flexible Display";
            this.Load += new System.EventHandler(this.FlexForm_Load);
            this.LayoutPanel.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
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
        private System.Windows.Forms.Button Undo;
        private System.Windows.Forms.Button Redo;
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.Integration.ElementHost RedoHost;
        private System.Windows.Forms.Integration.ElementHost UndoHost;
        private System.Windows.Forms.Label Information;
        private StackViewer RedoStack;
        private StackViewer UndoStack;
        private System.Windows.Forms.Button EditCml;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button ShowCml;
        private System.Windows.Forms.Button SaveStructure;
    }
}

