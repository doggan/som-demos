using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SOM_Color
{
	/// <summary>
	/// Summary description for Form2.
	/// </summary>
	public class form_About : System.Windows.Forms.Form
	{
        // ShellExecute used to open the website in default browser
        [DllImport("shell32.dll")]
        private static extern int ShellExecute(int hwnd, string lpOperation, string lpFile, string lpParams, string lpDir, int lpShowCmd);


        private System.Windows.Forms.Button button_OK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label_versionNumber;
        private System.Windows.Forms.LinkLabel linkLabel_url;
        private System.Windows.Forms.LinkLabel linkLabel_email;
        private System.Windows.Forms.Label label3;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public form_About()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.button_OK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.linkLabel_url = new System.Windows.Forms.LinkLabel();
            this.label_versionNumber = new System.Windows.Forms.Label();
            this.linkLabel_email = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button_OK
            // 
            this.button_OK.Location = new System.Drawing.Point(120, 120);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(72, 24);
            this.button_OK.TabIndex = 0;
            this.button_OK.Text = "OK";
            this.button_OK.Click += new System.EventHandler(this.button_OK_clicked);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(16, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "SOM Color demo by:";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(44, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Shyam M Guthikonda";
            // 
            // linkLabel_url
            // 
            this.linkLabel_url.Location = new System.Drawing.Point(44, 56);
            this.linkLabel_url.Name = "linkLabel_url";
            this.linkLabel_url.Size = new System.Drawing.Size(160, 23);
            this.linkLabel_url.TabIndex = 3;
            this.linkLabel_url.TabStop = true;
            this.linkLabel_url.Text = "http://www.ShyamMichael.com";
            this.linkLabel_url.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel_url_clicked);
            // 
            // label_versionNumber
            // 
            this.label_versionNumber.Location = new System.Drawing.Point(16, 96);
            this.label_versionNumber.Name = "label_versionNumber";
            this.label_versionNumber.Size = new System.Drawing.Size(176, 16);
            this.label_versionNumber.TabIndex = 4;
            this.label_versionNumber.Text = "Version Number: x.x";
            // 
            // linkLabel_email
            // 
            this.linkLabel_email.Location = new System.Drawing.Point(44, 72);
            this.linkLabel_email.Name = "linkLabel_email";
            this.linkLabel_email.Size = new System.Drawing.Size(128, 23);
            this.linkLabel_email.TabIndex = 5;
            this.linkLabel_email.TabStop = true;
            this.linkLabel_email.Text = "shyamguth@gmail.com";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(88, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 16);
            this.label3.TabIndex = 6;
            this.label3.Text = "(2005)";
            // 
            // form_About
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(208, 150);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.linkLabel_email);
            this.Controls.Add(this.label_versionNumber);
            this.Controls.Add(this.linkLabel_url);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "form_About";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About";
            this.Load += new System.EventHandler(this.form_About_Load);
            this.ResumeLayout(false);

        }
		#endregion

        private void form_About_Load(object sender, System.EventArgs e) {
            label_versionNumber.Text = "Version Number: " + Application.ProductVersion;
        }

        private void button_OK_clicked(object sender, System.EventArgs e) {
            this.Close();
        }

        private void linkLabel_url_clicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
            try {
                ShellExecute( 0, "open", linkLabel_url.Text, "", "", 1 );
            }
            catch {
                MessageBox.Show( "An error occurred while attempting to open up your web browser.", "Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
	}
}
