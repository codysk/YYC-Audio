
namespace YYC_Audio
{
    partial class MainFrame
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.actButton = new System.Windows.Forms.Button();
            this.deviceBox = new System.Windows.Forms.ComboBox();
            this.logBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // actButton
            // 
            this.actButton.Location = new System.Drawing.Point(342, 11);
            this.actButton.Name = "actButton";
            this.actButton.Size = new System.Drawing.Size(81, 21);
            this.actButton.TabIndex = 0;
            this.actButton.Text = "button1";
            this.actButton.UseVisualStyleBackColor = true;
            this.actButton.Click += new System.EventHandler(this.actButton_Click);
            // 
            // deviceBox
            // 
            this.deviceBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceBox.FormattingEnabled = true;
            this.deviceBox.Location = new System.Drawing.Point(12, 12);
            this.deviceBox.Name = "deviceBox";
            this.deviceBox.Size = new System.Drawing.Size(324, 20);
            this.deviceBox.TabIndex = 1;
            // 
            // logBox
            // 
            this.logBox.BackColor = System.Drawing.SystemColors.Window;
            this.logBox.Enabled = false;
            this.logBox.Location = new System.Drawing.Point(12, 39);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(411, 301);
            this.logBox.TabIndex = 2;
            // 
            // MainFrame
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(436, 352);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.deviceBox);
            this.Controls.Add(this.actButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "MainFrame";
            this.Text = "YYC-Audio";
            this.Load += new System.EventHandler(this.MainFrame_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button actButton;
        private System.Windows.Forms.ComboBox deviceBox;
        private System.Windows.Forms.TextBox logBox;
    }
}

