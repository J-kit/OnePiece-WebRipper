namespace OnePiece_WebRipper
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
			this.butDownload = new System.Windows.Forms.Button();
			this.dgvInfo = new System.Windows.Forms.DataGridView();
			this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dgvName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.url = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.butResolve = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dgvInfo)).BeginInit();
			this.SuspendLayout();
			// 
			// butDownload
			// 
			this.butDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.butDownload.Location = new System.Drawing.Point(299, 471);
			this.butDownload.Name = "butDownload";
			this.butDownload.Size = new System.Drawing.Size(246, 23);
			this.butDownload.TabIndex = 0;
			this.butDownload.Text = "Download";
			this.butDownload.UseVisualStyleBackColor = true;
			this.butDownload.Click += new System.EventHandler(this.butDownload_ClickAsync);
			// 
			// dgvInfo
			// 
			this.dgvInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dgvInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dgvInfo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.dgvName,
            this.url,
            this.Column2});
			this.dgvInfo.Location = new System.Drawing.Point(12, 12);
			this.dgvInfo.Name = "dgvInfo";
			this.dgvInfo.Size = new System.Drawing.Size(817, 453);
			this.dgvInfo.TabIndex = 1;
			// 
			// Column1
			// 
			this.Column1.HeaderText = "Folge";
			this.Column1.Name = "Column1";
			// 
			// dgvName
			// 
			this.dgvName.HeaderText = "Name";
			this.dgvName.Name = "dgvName";
			// 
			// url
			// 
			this.url.HeaderText = "Url";
			this.url.Name = "url";
			// 
			// Column2
			// 
			this.Column2.HeaderText = "percentage";
			this.Column2.Name = "Column2";
			// 
			// butResolve
			// 
			this.butResolve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.butResolve.Location = new System.Drawing.Point(33, 471);
			this.butResolve.Name = "butResolve";
			this.butResolve.Size = new System.Drawing.Size(246, 23);
			this.butResolve.TabIndex = 0;
			this.butResolve.Text = "Resolve";
			this.butResolve.UseVisualStyleBackColor = true;
			this.butResolve.Click += new System.EventHandler(this.butResolve_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(841, 506);
			this.Controls.Add(this.dgvInfo);
			this.Controls.Add(this.butResolve);
			this.Controls.Add(this.butDownload);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.dgvInfo)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butDownload;
		private System.Windows.Forms.DataGridView dgvInfo;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dgvName;
		private System.Windows.Forms.DataGridViewTextBoxColumn url;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
		private System.Windows.Forms.Button butResolve;
	}
}

