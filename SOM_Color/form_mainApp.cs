/*******************************************************************************/
/*  Author : Shyam M Guthikonda
/*  EMail  : shyamguth@gmail.com
/*  URL    : http://www.ShyamMichael.com
/*  Date   : 11 December 2005
/*  Desc.  : A self-organizing map (SOM) demo application written in C#. Classifies
/*           various input colors onto a 2-D network. See README.txt for usage
/*           notes.
/*******************************************************************************/

/* Additional Notes:
 * - All of the windows (rendered to) are assumed to be the same dimensions (square).
 *    Also, THEY MUST BE EVENLY DIVISIBLE by NUM_NODES_ACROSS and NUM_NODES_DOWN. If not,
 *    a round-off error will creep in (going from float to int) causing an odd grid-like
 *    pattern to appear in the windows.
 */

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace SOM_Color {
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class SOM_Color_Form : System.Windows.Forms.Form {
        // Some constants that may change from app to app.
        public const int NUM_WEIGHTS = 3;
        public const int NUM_NODES_ACROSS = 40;
        private System.Windows.Forms.StatusBar statusBar1;
        private System.Windows.Forms.Label label_IfGreyClickReset;
        public const int NUM_NODES_DOWN = 40;

        /* A struct to hold the most recent application settings (updated
         * when the "Reset" button is clicked).
         */
        public class AppSettings {
            public INIT_FILL initFill;
            public float width, height;
            public float nodeWidth, nodeHeight;     // Used to represent nodes as colored squares.
            public float startLearningRate;
            public int numIterations;
            public ArrayList inputVectors;          // Contains InputVectors
            public float mapRadius;

            public AppSettings() {
                inputVectors = new ArrayList();
            }
        }

        /* How we should initialize all of our net nodes.
         */
        public enum INIT_FILL {
            random,
            gradient,
            corners,
            triangle
        }

        public class InputVector {
            public float[] weights;

            public InputVector() {
                weights = new float[ SOM_Color_Form.NUM_WEIGHTS ];
            }

            public static InputVector operator +( InputVector v1, InputVector v2 ) {
                InputVector result = new InputVector();

                for ( int i = 0; i < SOM_Color_Form.NUM_WEIGHTS; ++i ) {
                    result.weights[ i ] = v1.weights[ i ] + v2.weights[ i ];
                }

                return result;
            }
        }

        /************************************************************************/
        /*                        Utility Functions (static)
        /************************************************************************/
        public static float util_randomFloatOneToZero() { return ( (float)m_random.Next(99999) / (float)99999.0 ); }
        public static int util_randomInt() { return m_random.Next(); }
        public static int util_randomInt( int maxVal ) { return m_random.Next( maxVal ); }
        public static int util_randomInt( int minVal, int maxVal ) { return m_random.Next( minVal, maxVal ); }
        public static int util_floatToByte( float color ) {
            int result = (int)(color * 255.0);

            // Guard against a reported (but not comfirmed) roundoff bug.
            if ( result < 0 ) result = 0;
            if ( result > 255 ) result = 255;

            return result;
        }
        public static float[] util_addWeights( float[] w1, float[] w2 ) {
            System.Diagnostics.Debug.Assert( w1.Length == w2.Length, "Trying to add 2 weights of unequal length!" );
            
            float[] result = new float[ w1.Length ];
            for ( int i = 0; i < result.Length; ++i )
                result[ i ] = w1[ i ] + w2[ i ];
            return result;
        }
        public static float util_getDistance( float[] p1, float[] p2, bool squareRoot ) {
            System.Diagnostics.Debug.Assert( p1.Length == p2.Length, "Trying to compute distance of 2 unequal element vectors!" );

            float distance = 0.0f;

            for ( int i = 0; i < p1.Length; ++i ) {
                distance += (p1[ i ] - p2[ i ]) *
                            (p1[ i ] - p2[ i ]);
            }

            if ( squareRoot )
                return (float)Math.Sqrt( distance );
            else
                return distance;
        }

        private const int NUM_WINDOWS = 4;
        private Graphics[] m_gWindow;
        private Graphics[] m_gOffscreenWindow;
        private Bitmap[] m_offscreenBitmap;
        private AppSettings m_appSettings;
        private CSOM m_SOM;
        private static Random m_random;
        private Timer m_timer;

        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.GroupBox groupBox_Init;
        private System.Windows.Forms.RadioButton radioButton_Random;
        private System.Windows.Forms.RadioButton radioButton_Gradient;
        private System.Windows.Forms.RadioButton radioButton_Corners;
        private System.Windows.Forms.MenuItem menuItem_File;
        private System.Windows.Forms.MenuItem menuItem_File_Quit;
        private System.Windows.Forms.MenuItem menuItem_Help;
        private System.Windows.Forms.MenuItem menuItem_Help_About;
        private System.Windows.Forms.Panel panel_Trained;
        private System.Windows.Forms.Panel panel_BMU;
        private System.Windows.Forms.Panel panel_Error;
        private System.Windows.Forms.Button button_Reset;
        private System.Windows.Forms.Button button_Train;
        private System.Windows.Forms.Panel panel_Init;
        private System.Windows.Forms.RadioButton radioButton_Triangle;
        private System.Windows.Forms.NumericUpDown numericUpDown_LearningRate;
        private System.Windows.Forms.NumericUpDown numericUpDown_Iterations;
        private System.Windows.Forms.Label label_LearningRate;
        private System.Windows.Forms.Label label_Iterations;
        private System.Windows.Forms.Label label_NumRandomColors;
        private System.Windows.Forms.CheckBox checkBox_UseRandomColors;
        private System.Windows.Forms.NumericUpDown numericUpDown_NumRandomColors;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public SOM_Color_Form() {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            m_random = new Random();
            
            m_timer = new Timer();
            m_timer.Enabled = true;
            m_timer.Interval = 1;
            m_timer.Tick += new System.EventHandler( timer_tick );

            numericUpDown_LearningRate.Value = (decimal).9;
            numericUpDown_Iterations.Value = 99;

            /* 0 = Initialization Window (top left)
             * 1 = Trained Window (top right)
             * 2 = "BMU The Most" Window (bottom left)
             * 3 = Map Error Window (bottom right)
             */
            m_gWindow = new Graphics[ NUM_WINDOWS ];
            m_gWindow[ 0 ] = panel_Init.CreateGraphics();
            m_gWindow[ 1 ] = panel_Trained.CreateGraphics();
            m_gWindow[ 2 ] = panel_BMU.CreateGraphics();
            m_gWindow[ 3 ] = panel_Error.CreateGraphics();

            m_offscreenBitmap = new Bitmap[ NUM_WINDOWS ];
            m_offscreenBitmap[ 0 ] = new Bitmap( panel_Init.Width, panel_Init.Height );
            m_offscreenBitmap[ 1 ] = new Bitmap( panel_Trained.Width, panel_Trained.Height );
            m_offscreenBitmap[ 2 ] = new Bitmap( panel_BMU.Width, panel_BMU.Height );
            m_offscreenBitmap[ 3 ] = new Bitmap( panel_Error.Width, panel_Error.Height );
            m_gOffscreenWindow = new Graphics[ NUM_WINDOWS ];
            m_gOffscreenWindow[ 0 ] = Graphics.FromImage( m_offscreenBitmap[ 0 ] );
            m_gOffscreenWindow[ 1 ] = Graphics.FromImage( m_offscreenBitmap[ 1 ] );
            m_gOffscreenWindow[ 2 ] = Graphics.FromImage( m_offscreenBitmap[ 2 ] );
            m_gOffscreenWindow[ 3 ] = Graphics.FromImage( m_offscreenBitmap[ 3 ] );

            m_appSettings = new AppSettings();

            onReset();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing ) {
            for ( int i = 0; i < NUM_WINDOWS; ++i ) {
                m_gWindow[ i ].Dispose();
            }

            if( disposing ) {
                if (components != null) {
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
        private void InitializeComponent() {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SOM_Color_Form));
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.menuItem_File = new System.Windows.Forms.MenuItem();
            this.menuItem_File_Quit = new System.Windows.Forms.MenuItem();
            this.menuItem_Help = new System.Windows.Forms.MenuItem();
            this.menuItem_Help_About = new System.Windows.Forms.MenuItem();
            this.groupBox_Init = new System.Windows.Forms.GroupBox();
            this.radioButton_Triangle = new System.Windows.Forms.RadioButton();
            this.radioButton_Corners = new System.Windows.Forms.RadioButton();
            this.radioButton_Gradient = new System.Windows.Forms.RadioButton();
            this.radioButton_Random = new System.Windows.Forms.RadioButton();
            this.panel_Trained = new System.Windows.Forms.Panel();
            this.panel_BMU = new System.Windows.Forms.Panel();
            this.panel_Error = new System.Windows.Forms.Panel();
            this.button_Reset = new System.Windows.Forms.Button();
            this.button_Train = new System.Windows.Forms.Button();
            this.panel_Init = new System.Windows.Forms.Panel();
            this.numericUpDown_LearningRate = new System.Windows.Forms.NumericUpDown();
            this.label_LearningRate = new System.Windows.Forms.Label();
            this.label_Iterations = new System.Windows.Forms.Label();
            this.numericUpDown_Iterations = new System.Windows.Forms.NumericUpDown();
            this.checkBox_UseRandomColors = new System.Windows.Forms.CheckBox();
            this.numericUpDown_NumRandomColors = new System.Windows.Forms.NumericUpDown();
            this.label_NumRandomColors = new System.Windows.Forms.Label();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.label_IfGreyClickReset = new System.Windows.Forms.Label();
            this.groupBox_Init.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LearningRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Iterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_NumRandomColors)).BeginInit();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                      this.menuItem_File,
                                                                                      this.menuItem_Help});
            // 
            // menuItem_File
            // 
            this.menuItem_File.Index = 0;
            this.menuItem_File.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                          this.menuItem_File_Quit});
            this.menuItem_File.Text = "File";
            // 
            // menuItem_File_Quit
            // 
            this.menuItem_File_Quit.Index = 0;
            this.menuItem_File_Quit.Text = "Quit";
            this.menuItem_File_Quit.Click += new System.EventHandler(this.menu_File_Quit);
            // 
            // menuItem_Help
            // 
            this.menuItem_Help.Index = 1;
            this.menuItem_Help.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                          this.menuItem_Help_About});
            this.menuItem_Help.Text = "Help";
            // 
            // menuItem_Help_About
            // 
            this.menuItem_Help_About.Index = 0;
            this.menuItem_Help_About.Text = "About";
            this.menuItem_Help_About.Click += new System.EventHandler(this.menu_Help_About);
            // 
            // groupBox_Init
            // 
            this.groupBox_Init.Controls.Add(this.radioButton_Triangle);
            this.groupBox_Init.Controls.Add(this.radioButton_Corners);
            this.groupBox_Init.Controls.Add(this.radioButton_Gradient);
            this.groupBox_Init.Controls.Add(this.radioButton_Random);
            this.groupBox_Init.Location = new System.Drawing.Point(448, 8);
            this.groupBox_Init.Name = "groupBox_Init";
            this.groupBox_Init.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.groupBox_Init.Size = new System.Drawing.Size(192, 80);
            this.groupBox_Init.TabIndex = 0;
            this.groupBox_Init.TabStop = false;
            this.groupBox_Init.Text = "Initializations";
            // 
            // radioButton_Triangle
            // 
            this.radioButton_Triangle.Enabled = false;
            this.radioButton_Triangle.Location = new System.Drawing.Point(96, 48);
            this.radioButton_Triangle.Name = "radioButton_Triangle";
            this.radioButton_Triangle.Size = new System.Drawing.Size(88, 24);
            this.radioButton_Triangle.TabIndex = 3;
            this.radioButton_Triangle.Text = "Triangle";
            this.radioButton_Triangle.CheckedChanged += new System.EventHandler(this.radioButton_Triangle_CheckedChanged);
            // 
            // radioButton_Corners
            // 
            this.radioButton_Corners.Location = new System.Drawing.Point(96, 24);
            this.radioButton_Corners.Name = "radioButton_Corners";
            this.radioButton_Corners.Size = new System.Drawing.Size(88, 24);
            this.radioButton_Corners.TabIndex = 2;
            this.radioButton_Corners.Text = "Corners";
            this.radioButton_Corners.CheckedChanged += new System.EventHandler(this.radioButton_Corners_CheckedChanged);
            // 
            // radioButton_Gradient
            // 
            this.radioButton_Gradient.Location = new System.Drawing.Point(16, 48);
            this.radioButton_Gradient.Name = "radioButton_Gradient";
            this.radioButton_Gradient.Size = new System.Drawing.Size(72, 24);
            this.radioButton_Gradient.TabIndex = 1;
            this.radioButton_Gradient.Text = "Gradient";
            this.radioButton_Gradient.CheckedChanged += new System.EventHandler(this.radioButton_Gradient_CheckedChanged);
            // 
            // radioButton_Random
            // 
            this.radioButton_Random.Checked = true;
            this.radioButton_Random.Location = new System.Drawing.Point(16, 24);
            this.radioButton_Random.Name = "radioButton_Random";
            this.radioButton_Random.Size = new System.Drawing.Size(72, 24);
            this.radioButton_Random.TabIndex = 0;
            this.radioButton_Random.TabStop = true;
            this.radioButton_Random.Text = "Random";
            this.radioButton_Random.CheckedChanged += new System.EventHandler(this.radioButton_Random_CheckedChanged);
            // 
            // panel_Trained
            // 
            this.panel_Trained.BackColor = System.Drawing.SystemColors.Control;
            this.panel_Trained.Location = new System.Drawing.Point(504, 96);
            this.panel_Trained.Name = "panel_Trained";
            this.panel_Trained.Size = new System.Drawing.Size(280, 280);
            this.panel_Trained.TabIndex = 2;
            // 
            // panel_BMU
            // 
            this.panel_BMU.BackColor = System.Drawing.SystemColors.Control;
            this.panel_BMU.Location = new System.Drawing.Point(216, 384);
            this.panel_BMU.Name = "panel_BMU";
            this.panel_BMU.Size = new System.Drawing.Size(280, 280);
            this.panel_BMU.TabIndex = 3;
            // 
            // panel_Error
            // 
            this.panel_Error.BackColor = System.Drawing.SystemColors.Control;
            this.panel_Error.Location = new System.Drawing.Point(504, 384);
            this.panel_Error.Name = "panel_Error";
            this.panel_Error.Size = new System.Drawing.Size(280, 280);
            this.panel_Error.TabIndex = 4;
            // 
            // button_Reset
            // 
            this.button_Reset.Location = new System.Drawing.Point(576, 694);
            this.button_Reset.Name = "button_Reset";
            this.button_Reset.Size = new System.Drawing.Size(75, 24);
            this.button_Reset.TabIndex = 5;
            this.button_Reset.Text = "Reset";
            this.button_Reset.Click += new System.EventHandler(this.clicked_ButtonReset);
            // 
            // button_Train
            // 
            this.button_Train.Enabled = false;
            this.button_Train.Location = new System.Drawing.Point(672, 694);
            this.button_Train.Name = "button_Train";
            this.button_Train.Size = new System.Drawing.Size(75, 24);
            this.button_Train.TabIndex = 6;
            this.button_Train.Text = "Train";
            this.button_Train.Click += new System.EventHandler(this.clicked_ButtonTrain);
            // 
            // panel_Init
            // 
            this.panel_Init.BackColor = System.Drawing.SystemColors.Control;
            this.panel_Init.Location = new System.Drawing.Point(216, 96);
            this.panel_Init.Name = "panel_Init";
            this.panel_Init.Size = new System.Drawing.Size(280, 280);
            this.panel_Init.TabIndex = 1;
            // 
            // numericUpDown_LearningRate
            // 
            this.numericUpDown_LearningRate.DecimalPlaces = 2;
            this.numericUpDown_LearningRate.Increment = new System.Decimal(new int[] {
                                                                                         1,
                                                                                         0,
                                                                                         0,
                                                                                         131072});
            this.numericUpDown_LearningRate.Location = new System.Drawing.Point(96, 488);
            this.numericUpDown_LearningRate.Maximum = new System.Decimal(new int[] {
                                                                                       1,
                                                                                       0,
                                                                                       0,
                                                                                       0});
            this.numericUpDown_LearningRate.Minimum = new System.Decimal(new int[] {
                                                                                       1,
                                                                                       0,
                                                                                       0,
                                                                                       131072});
            this.numericUpDown_LearningRate.Name = "numericUpDown_LearningRate";
            this.numericUpDown_LearningRate.Size = new System.Drawing.Size(64, 20);
            this.numericUpDown_LearningRate.TabIndex = 7;
            this.numericUpDown_LearningRate.Value = new System.Decimal(new int[] {
                                                                                     1,
                                                                                     0,
                                                                                     0,
                                                                                     131072});
            this.numericUpDown_LearningRate.ValueChanged += new System.EventHandler(this.numericUpDown_LearningRate_ValueChanged);
            // 
            // label_LearningRate
            // 
            this.label_LearningRate.Location = new System.Drawing.Point(56, 464);
            this.label_LearningRate.Name = "label_LearningRate";
            this.label_LearningRate.Size = new System.Drawing.Size(80, 16);
            this.label_LearningRate.TabIndex = 8;
            this.label_LearningRate.Text = "Learning Rate:";
            // 
            // label_Iterations
            // 
            this.label_Iterations.Location = new System.Drawing.Point(56, 528);
            this.label_Iterations.Name = "label_Iterations";
            this.label_Iterations.Size = new System.Drawing.Size(56, 16);
            this.label_Iterations.TabIndex = 9;
            this.label_Iterations.Text = "Iterations:";
            // 
            // numericUpDown_Iterations
            // 
            this.numericUpDown_Iterations.Location = new System.Drawing.Point(96, 552);
            this.numericUpDown_Iterations.Maximum = new System.Decimal(new int[] {
                                                                                     1000,
                                                                                     0,
                                                                                     0,
                                                                                     0});
            this.numericUpDown_Iterations.Minimum = new System.Decimal(new int[] {
                                                                                     1,
                                                                                     0,
                                                                                     0,
                                                                                     0});
            this.numericUpDown_Iterations.Name = "numericUpDown_Iterations";
            this.numericUpDown_Iterations.Size = new System.Drawing.Size(64, 20);
            this.numericUpDown_Iterations.TabIndex = 10;
            this.numericUpDown_Iterations.Value = new System.Decimal(new int[] {
                                                                                   1,
                                                                                   0,
                                                                                   0,
                                                                                   0});
            this.numericUpDown_Iterations.ValueChanged += new System.EventHandler(this.numericUpDown_Iterations_ValueChanged);
            // 
            // checkBox_UseRandomColors
            // 
            this.checkBox_UseRandomColors.Location = new System.Drawing.Point(240, 32);
            this.checkBox_UseRandomColors.Name = "checkBox_UseRandomColors";
            this.checkBox_UseRandomColors.Size = new System.Drawing.Size(154, 24);
            this.checkBox_UseRandomColors.TabIndex = 11;
            this.checkBox_UseRandomColors.Text = "Use Random Input Colors";
            this.checkBox_UseRandomColors.CheckedChanged += new System.EventHandler(this.checkBox_UseRandomColors_CheckedChanged);
            // 
            // numericUpDown_NumRandomColors
            // 
            this.numericUpDown_NumRandomColors.Location = new System.Drawing.Point(344, 59);
            this.numericUpDown_NumRandomColors.Maximum = new System.Decimal(new int[] {
                                                                                          20,
                                                                                          0,
                                                                                          0,
                                                                                          0});
            this.numericUpDown_NumRandomColors.Minimum = new System.Decimal(new int[] {
                                                                                          1,
                                                                                          0,
                                                                                          0,
                                                                                          0});
            this.numericUpDown_NumRandomColors.Name = "numericUpDown_NumRandomColors";
            this.numericUpDown_NumRandomColors.Size = new System.Drawing.Size(48, 20);
            this.numericUpDown_NumRandomColors.TabIndex = 12;
            this.numericUpDown_NumRandomColors.Value = new System.Decimal(new int[] {
                                                                                        1,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            this.numericUpDown_NumRandomColors.ValueChanged += new System.EventHandler(this.numericUpDown_NumRandomColors_ValueChanged);
            // 
            // label_NumRandomColors
            // 
            this.label_NumRandomColors.Location = new System.Drawing.Point(232, 56);
            this.label_NumRandomColors.Name = "label_NumRandomColors";
            this.label_NumRandomColors.Size = new System.Drawing.Size(112, 23);
            this.label_NumRandomColors.TabIndex = 13;
            this.label_NumRandomColors.Text = "# of Random Colors:";
            this.label_NumRandomColors.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 721);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Size = new System.Drawing.Size(790, 22);
            this.statusBar1.TabIndex = 14;
            this.statusBar1.Text = "Network Status: Neutral";
            // 
            // label_IfGreyClickReset
            // 
            this.label_IfGreyClickReset.Location = new System.Drawing.Point(656, 670);
            this.label_IfGreyClickReset.Name = "label_IfGreyClickReset";
            this.label_IfGreyClickReset.Size = new System.Drawing.Size(112, 16);
            this.label_IfGreyClickReset.TabIndex = 15;
            this.label_IfGreyClickReset.Text = "(If grey, click Reset).";
            // 
            // SOM_Color_Form
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(790, 743);
            this.Controls.Add(this.label_IfGreyClickReset);
            this.Controls.Add(this.statusBar1);
            this.Controls.Add(this.label_NumRandomColors);
            this.Controls.Add(this.numericUpDown_NumRandomColors);
            this.Controls.Add(this.checkBox_UseRandomColors);
            this.Controls.Add(this.numericUpDown_Iterations);
            this.Controls.Add(this.label_Iterations);
            this.Controls.Add(this.label_LearningRate);
            this.Controls.Add(this.numericUpDown_LearningRate);
            this.Controls.Add(this.button_Train);
            this.Controls.Add(this.button_Reset);
            this.Controls.Add(this.panel_Error);
            this.Controls.Add(this.panel_BMU);
            this.Controls.Add(this.panel_Trained);
            this.Controls.Add(this.panel_Init);
            this.Controls.Add(this.groupBox_Init);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Menu = this.mainMenu1;
            this.Name = "SOM_Color_Form";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Text = "SOM_Color";
            this.Load += new System.EventHandler(this.SOM_Color_Form_Load);
            this.groupBox_Init.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LearningRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Iterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_NumRandomColors)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.Run(new SOM_Color_Form());
        }

        private void SOM_Color_Form_Load(object sender, System.EventArgs e) {
        
        }

        private void timer_tick( object sender, System.EventArgs e ) {
            Invalidate();
        }

        private void getInputVectors() {
            m_appSettings.inputVectors.Clear();

            // If "Use Random Colors" is checked, get the # of colors to use
            //  and calculate some random colors.
            if ( checkBox_UseRandomColors.Checked ) {
                int numRandomColors = Decimal.ToInt32( numericUpDown_NumRandomColors.Value );

                for ( int i = 0; i < numRandomColors; ++i ) {
                    InputVector inputColor = new InputVector();
                    inputColor.weights[ 0 ] = util_randomFloatOneToZero();  // R
                    inputColor.weights[ 1 ] = util_randomFloatOneToZero();  // G
                    inputColor.weights[ 2 ] = util_randomFloatOneToZero();  // B
                    m_appSettings.inputVectors.Add( inputColor );
                }
            }
            // Just use preset colors.
            else {
                InputVector inputColor;

                // Red.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 1.0f ;
                inputColor.weights[ 1 ] = 0.0f;
                inputColor.weights[ 2 ] = 0.0f;
                m_appSettings.inputVectors.Add( inputColor );

                // Green.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 0.0f;
                inputColor.weights[ 1 ] = 1.0f;
                inputColor.weights[ 2 ] = 0.0f;
                m_appSettings.inputVectors.Add( inputColor );

                // Blue.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 0.0f;
                inputColor.weights[ 1 ] = 0.0f;
                inputColor.weights[ 2 ] = 1.0f;
                m_appSettings.inputVectors.Add( inputColor );

                // Dark green.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 0.0f;
                inputColor.weights[ 1 ] = 0.5f;
                inputColor.weights[ 2 ] = 0.25f;
                m_appSettings.inputVectors.Add( inputColor );

                // Dark blue.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 0.0f;
                inputColor.weights[ 1 ] = 0.0f;
                inputColor.weights[ 2 ] = 0.5f;
                m_appSettings.inputVectors.Add( inputColor );

                // Yellow.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 1.0f;
                inputColor.weights[ 1 ] = 1.0f;
                inputColor.weights[ 2 ] = 0.2f;
                m_appSettings.inputVectors.Add( inputColor );

                // Orange.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 1.0f;
                inputColor.weights[ 1 ] = 0.4f;
                inputColor.weights[ 2 ] = 0.25f;
                m_appSettings.inputVectors.Add( inputColor );

                // Purple.
                inputColor = new InputVector();
                inputColor.weights[ 0 ] = 1.0f;
                inputColor.weights[ 1 ] = 0.0f;
                inputColor.weights[ 2 ] = 1.0f;
                m_appSettings.inputVectors.Add( inputColor );
            }
        }

        /* This method is called on app initialization and whenever the RESET
         * button is clicked. It will fill out the AppSettings struct with the
         * current settings.
         */
        private void updateAppSettings() {
            getInputVectors();

            // Initialization fill.
            if ( radioButton_Random.Checked )
                m_appSettings.initFill = INIT_FILL.random;
            else if ( radioButton_Gradient.Checked )
                m_appSettings.initFill = INIT_FILL.gradient;
            else if ( radioButton_Corners.Checked )
                m_appSettings.initFill = INIT_FILL.corners;
            else if ( radioButton_Triangle.Checked )
                m_appSettings.initFill = INIT_FILL.triangle;

            // Assumes all 4 rendering windows are the same dimensions.
            Size s = panel_Init.Size;
            m_appSettings.width = (float)s.Width;
            m_appSettings.height = (float)s.Height;

            // Calculate node square dimensions.
            m_appSettings.nodeWidth = m_appSettings.width / (float)SOM_Color_Form.NUM_NODES_ACROSS;
            m_appSettings.nodeHeight = m_appSettings.height / (float)SOM_Color_Form.NUM_NODES_DOWN;

            m_appSettings.mapRadius = Math.Max( m_appSettings.width, m_appSettings.height ) / 2.0f;

            m_appSettings.startLearningRate = (float)Decimal.ToDouble( numericUpDown_LearningRate.Value );

            m_appSettings.numIterations = Decimal.ToInt32( numericUpDown_Iterations.Value );
        }

        protected override void OnPaint(PaintEventArgs pe) {
            if ( m_SOM == null )
                return;

            // Update the status bar.
            float tme = m_SOM.getTotalMapError();
            if ( tme <= 0.0f )  this.statusBar1.Text = m_SOM.getNetState() + " Total Map Error: -";
            else                this.statusBar1.Text = m_SOM.getNetState() + " Total Map Error: " + tme.ToString();

            m_SOM.render( m_gWindow, m_gOffscreenWindow, m_offscreenBitmap );
        }

        private void menu_File_Quit(object sender, System.EventArgs e) {
            Application.Exit();
        }

        private void menu_Help_About(object sender, System.EventArgs e) {
            SOM_Color.form_About a = new form_About();
            a.Icon = this.Icon;
            a.Show();
        }

        private void clicked_ButtonReset(object sender, System.EventArgs e) {
            onReset();
        }

        private void onReset() {
            if ( m_SOM != null )
                m_SOM.stopTraining();
            updateAppSettings();
            m_SOM = new CSOM( m_appSettings );
            button_Train.Enabled = true;
        }

        private void clicked_ButtonTrain(object sender, System.EventArgs e) {
            if ( !m_SOM.isTraining() )
                m_SOM.startTraining();
        }

        /* If a value is changed, user must click reset before clicking train.
         */
        private void mustReset() {
            button_Train.Enabled = false;
        }

        private void numericUpDown_LearningRate_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void numericUpDown_Iterations_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void checkBox_UseRandomColors_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
        private void numericUpDown_NumRandomColors_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void radioButton_Random_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
        private void radioButton_Gradient_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
        private void radioButton_Corners_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
        private void radioButton_Triangle_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
    }

    /* Node class. Used to represent a single weighted node in the network.
     */
    public class CNode {
        private int m_i, m_j;
        private float[] m_weights;
        private int m_bmuCount;

        public CNode( int i, int j ) {
            m_i = i; m_j = j;
            m_weights = new float[ SOM_Color_Form.NUM_WEIGHTS ];
            m_bmuCount = 0;
        }

        public void setWeights( float [] weight ) {
            for ( int i = 0; i < weight.Length; ++i )
                m_weights[ i ] = weight[ i ];
        }

        public float[] getWeights() { return m_weights; }

        public void incrementBMUCount() { ++m_bmuCount; }
        public void zeroBMUCount() { m_bmuCount = 0; }
        public int getBMUCount() { return m_bmuCount; }

        public int getI() { return m_i; }
        public int getJ() { return m_j; }

        public float calculateDistanceSquared( SOM_Color_Form.InputVector inputVec ) {
            float distance = 0.0f;

            for ( int i = 0; i < inputVec.weights.Length; ++i ) {
                distance += (inputVec.weights[ i ] - m_weights[ i ]) *
                            (inputVec.weights[ i ] - m_weights[ i ]);
            }

            return distance;
        }

        /* Calculates the new weight for this node.
         * Equation: W(t+1) = W(t) + THETA(t)*L(t)*(V(t) - W(t))
         */ 
        public void adjustWeights( SOM_Color_Form.InputVector targetVector, float learningRate, float influence ) {
            for ( int w = 0; w < targetVector.weights.Length; ++w ) {
                m_weights[ w ] += learningRate * influence * (targetVector.weights[ w ] - m_weights[ w ]);
            }
        }
    }

    /* SOM class. Encapsulates all SOM logic.
     */
    public class CSOM {
        private SOM_Color_Form.AppSettings m_appSettings;
        private CNode[,] m_network;
        private NET_STATE m_netState;
        private int m_currentIteration;
        private float m_neighborhoodRadius;
        private float m_timeConstant;
        private float m_learningRate;
        private float m_totalMapError;

        /*  Used to hold color values for 3 of the 4 windows (window #1 stores it's
         *  values as the weight of each node in the network). The 4 dimensional array
         *  was avoided as the windows each behave differently on that level.
         *
         *  m_window00 = initialization window.
         *  m_window02 = BMU window.
         *  m_window03 = Error map window.
         */
        private float[,,] m_window00, m_window02, m_window03;

        private enum NET_STATE {
            neutral,    // Nothing needs to be done. Waiting.
            init,       // We need to initialize all the windows.
            training,   // Need to update (window 2) to show progress.
            finished    // Display the results (calculate windows 3 and 4).
        }

        public float getTotalMapError() { return m_totalMapError; }

        public string getNetState() {
            if ( m_netState == NET_STATE.finished ) {
                return "Network Status: Finished...";
            }
            else if ( m_netState == NET_STATE.init ) {
                return "Network Status: Initializing...";
            }
            else if ( m_netState == NET_STATE.training ) {
                return "Network Status: Training... Iteration #" + (m_currentIteration - 1).ToString();
            }
            else {
                return "Network Status: Neutral.";
            }
        }

        public bool isTraining() {
            if ( m_netState == NET_STATE.init ||
                 m_netState == NET_STATE.training ||
                 m_netState == NET_STATE.finished )
                return true;
            else
                return false;
        }

        public void startTraining() {
            m_netState = NET_STATE.training;
        }
        public void stopTraining() {
            m_netState = NET_STATE.neutral;
        }

        public CSOM( SOM_Color_Form.AppSettings appSettings ) {
            m_appSettings = appSettings;
            m_netState = NET_STATE.init;
            m_currentIteration = 1;
            m_totalMapError = 0.0f;

            m_timeConstant = (float)m_appSettings.numIterations / (float)Math.Log( m_appSettings.mapRadius );
            m_learningRate = m_appSettings.startLearningRate;

            // Allocate memory.
            m_network = new CNode[ SOM_Color_Form.NUM_NODES_DOWN, SOM_Color_Form.NUM_NODES_ACROSS ];
            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                    m_network[ i,j ] = new CNode( i, j );
                }
            }
            m_window00 = new float[ SOM_Color_Form.NUM_NODES_DOWN, SOM_Color_Form.NUM_NODES_ACROSS, SOM_Color_Form.NUM_WEIGHTS ];
            m_window02 = new float[ SOM_Color_Form.NUM_NODES_DOWN, SOM_Color_Form.NUM_NODES_ACROSS, SOM_Color_Form.NUM_WEIGHTS ];
            m_window03 = new float[ SOM_Color_Form.NUM_NODES_DOWN, SOM_Color_Form.NUM_NODES_ACROSS, SOM_Color_Form.NUM_WEIGHTS ];

            calcWindows();
        }

        /* This function calls calcWindows() to get the latest values for all four
         * windows. It then proceeds to render all the squares (nodes) in all four
         * windows.
         */
        public void render( Graphics[] theWindows, Graphics[] offscreenWindows, Bitmap[] offscreenBitmaps ) {
            calcWindows();

            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                    float[] weights0, weights1, weights2, weights3;
                    weights0 = new float[ SOM_Color_Form.NUM_WEIGHTS ];
                    weights1 = m_network[ i,j ].getWeights();
                    weights2 = new float[ SOM_Color_Form.NUM_WEIGHTS ];
                    weights3 = new float[ SOM_Color_Form.NUM_WEIGHTS ];

                    for ( int w = 0; w < SOM_Color_Form.NUM_WEIGHTS; ++w ) {
                        weights0[ w ] = m_window00[ i,j,w ];
                        weights2[ w ] = m_window02[ i,j,w ];
                        weights3[ w ] = m_window03[ i,j,w ];
                    }

                    Rectangle rect = new Rectangle( 
                        (int)(m_appSettings.nodeWidth * i), (int)(m_appSettings.nodeHeight * j), (int)m_appSettings.nodeWidth, (int)m_appSettings.nodeHeight
                        );

                    SolidBrush brush0, brush1, brush2, brush3;
                    brush0 = new SolidBrush( Color.FromArgb(
                        SOM_Color_Form.util_floatToByte( weights0[ 0 ]),
                        SOM_Color_Form.util_floatToByte( weights0[ 1 ]), 
                        SOM_Color_Form.util_floatToByte( weights0[ 2 ]) )
                        );
                    brush1 = new SolidBrush( Color.FromArgb(
                        SOM_Color_Form.util_floatToByte( weights1[ 0 ]), 
                        SOM_Color_Form.util_floatToByte( weights1[ 1 ]), 
                        SOM_Color_Form.util_floatToByte( weights1[ 2 ]) )
                        );
                    brush2 = new SolidBrush( Color.FromArgb(
                        SOM_Color_Form.util_floatToByte( weights2[ 0 ]), 
                        SOM_Color_Form.util_floatToByte( weights2[ 1 ]), 
                        SOM_Color_Form.util_floatToByte( weights2[ 2 ]) )
                        );                            
                    brush3 = new SolidBrush( Color.FromArgb(
                        SOM_Color_Form.util_floatToByte( weights3[ 0 ]), 
                        SOM_Color_Form.util_floatToByte( weights3[ 1 ]), 
                        SOM_Color_Form.util_floatToByte( weights3[ 2 ]) )
                        );

                    offscreenWindows[ 0 ].FillRectangle( brush0, rect );
                    offscreenWindows[ 1 ].FillRectangle( brush1, rect );
                    offscreenWindows[ 2 ].FillRectangle( brush2, rect );
                    offscreenWindows[ 3 ].FillRectangle( brush3, rect );
                } // End for each column.
            } // End for each row.

            theWindows[ 0 ].DrawImage( offscreenBitmaps[ 0 ], 0, 0 );
            theWindows[ 1 ].DrawImage( offscreenBitmaps[ 1 ], 0, 0 );
            theWindows[ 2 ].DrawImage( offscreenBitmaps[ 2 ], 0, 0 );
            theWindows[ 3 ].DrawImage( offscreenBitmaps[ 3 ], 0, 0 );
        }

        /* Based on the current state of the network, this function calculates
         * the values to be displayed in all four windows. It will shift states
         * as needed.
         */
        private void calcWindows() {
            switch ( m_netState ) {
                case NET_STATE.neutral: {
                    break;
                }
                case NET_STATE.init: {
                    windowsInit();

                    m_netState = NET_STATE.neutral;
                    break;
                }
                case NET_STATE.training: {
                    if ( windowsEpoch() )
                        m_netState = NET_STATE.finished;

                    break;
                }
                case NET_STATE.finished: {
                    windowsBMU();
                    m_totalMapError = windowsErrorMap();

                    m_netState = NET_STATE.neutral;
                    break;
                }
            } // End switch.
        }

        /* This function determines the values of the windows on initialization. It's
         * called on app startup, and every time the reset button is hit. window01 has
         * it's values calculated based on our initialization method. window02 is set
         * to the values of window01. window02 and window03 are just set to black.
         */
        private void windowsInit() {
            float[] weight = new float[ SOM_Color_Form.NUM_WEIGHTS ];

            /************************************************************************/
            /* STEP 1: Each nodes' weights are initialized.                                                           
            /************************************************************************/
            switch ( m_appSettings.initFill ) {
                case SOM_Color_Form.INIT_FILL.random: {
                    // Initialize all weights to random floats (0.0 to 1.0).
                    for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                        for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {

                            CNode currentNode = m_network[ i,j ];
                            
                            for ( int w = 0; w < SOM_Color_Form.NUM_WEIGHTS; ++w ) {
                                weight[ w ] = SOM_Color_Form.util_randomFloatOneToZero();
                                m_window00[ i,j,w ] = weight[ w ];
                                m_window02[ i,j,w ] = m_window03[ i,j,w ] = 0.0f; // Black.
                            }
                            currentNode.setWeights( weight );
                        } // End for each column.
                    } // End for each row.
                    break;
                }
                case SOM_Color_Form.INIT_FILL.gradient: {
                    for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                        for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                            CNode currentNode = m_network[ i,j ];

                            // **NOTE** Assumes SOM_COLOR
                            float gradVal = (float)(i + j) / (float)(SOM_Color_Form.NUM_NODES_DOWN + SOM_Color_Form.NUM_NODES_ACROSS - 2);
                            for ( int w = 0; w < SOM_Color_Form.NUM_WEIGHTS; ++w ) {
                                weight[ w ] = m_window00[ i,j,w ] = gradVal;
                                m_window02[ i,j,w ] = m_window03[ i,j,w ] = 0.0f; // Black.
                            }
                            currentNode.setWeights( weight );
                        } // End for each column.
                    } // End for each row.
                    break;
                }
                case SOM_Color_Form.INIT_FILL.corners: {
                    float Hmul, Wmul;
                    for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                        Hmul = ((float)i / (float)SOM_Color_Form.NUM_NODES_DOWN) * 7.0f;
                        for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                            Wmul = ((float)j / (float)SOM_Color_Form.NUM_NODES_ACROSS);

                            CNode currentNode = m_network[ i,j ];

                            for ( int w = 0; w < SOM_Color_Form.NUM_WEIGHTS; ++w ) {
                                // **NOTE** Assumes SOM_COLOR
                                if ( w == 0 ) weight[ w ] = ((1.0f - Wmul) * Hmul)/7.0f;
                                else if ( w == 1 ) weight[ w ] = (Wmul * Hmul)/7.0f;
                                else if ( w == 2 ) weight[ w ] = (Math.Abs( Wmul ) * (7.0f - Hmul))/7.0f;
                                m_window00[ i,j,w ] = weight[ w ];
                                m_window02[ i,j,w ] = m_window03[ i,j,w ] = 0.0f; // Black.
                            }
                            currentNode.setWeights( weight );
                        } // End for each column.
                    } // End for each row.
                    break;
                }
                case SOM_Color_Form.INIT_FILL.triangle: {

                    float[] center = new float[ 3 ]; center[ 0 ] = m_appSettings.width; center[ 1 ] = m_appSettings.height; center[ 2 ] = 0.0f;
                    float[] outer = new float[ 3 ]; outer[ 0 ] = 0.0f; outer[ 1 ] = 0.0f; outer[ 2 ] = 0.0f;

                    float max_dist = SOM_Color_Form.util_getDistance( center, outer, true );
                    float theta1 = 90.0f * ((float)Math.PI / 180.0f);
                    float theta2 = 210.0f * ((float)Math.PI / 180.0f);
                    float theta3 = 330.0f * ((float)Math.PI / 180.0f);
                    float H2 = (float)m_appSettings.height/2.0f;
                    float H4 = (float)m_appSettings.height/4.0f;
                    float W2 = (float)m_appSettings.width/2.0f;

                    float[] rcenter = new float[ 3 ];
                    rcenter[ 0 ] = (float)(Math.Cos(theta1))*H4 + W2;
                    rcenter[ 1 ] = (float)(Math.Sin(theta1))*H4 + H2;
                    rcenter[ 2 ] = 0.0f;
                    float[] gcenter = new float[ 3 ];
                    gcenter[ 0 ] = (float)(Math.Cos(theta2))*H4 + W2;
                    gcenter[ 1 ] = (float)(Math.Sin(theta2))*H4 + H2;
                    gcenter[ 2 ] = 0.0f;
                    float[] bcenter = new float[ 3 ];
                    bcenter[ 0 ] = (float)(Math.Cos(theta3))*H4 + W2;
                    bcenter[ 1 ] = (float)(Math.Sin(theta3))*H4 + H2;
                    bcenter[ 2 ] = 0.0f;

                    for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                        for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                            outer[ 0 ] = (float)j;
                            outer[ 1 ] = (float)i;

                            CNode currentNode = m_network[ i,j ];

                            // **NOTE** Assumes SOM_COLOR
                            weight[ 0 ] = m_window00[ i,j,0 ] = SOM_Color_Form.util_getDistance( outer, rcenter, true ) / max_dist;
                            weight[ 1 ] = m_window00[ i,j,1 ] = SOM_Color_Form.util_getDistance( outer, gcenter, true ) / max_dist;
                            weight[ 2 ] = m_window00[ i,j,2 ] = SOM_Color_Form.util_getDistance( outer, bcenter, true ) / max_dist;

                            for ( int w = 0; w < SOM_Color_Form.NUM_WEIGHTS; ++w ) {
                                m_window02[ i,j,w ] = m_window03[ i,j,w ] = 0.0f; // Black.
                            }

                            currentNode.setWeights( weight );
                        } // End for each column.
                    } // End for each row.

                    break;
                }
            } // End switch.
        }

        /* This is the function that performs the network training. It will return
         * true when finished training, false if not.
         */
        private bool windowsEpoch() {
            if ( m_currentIteration > m_appSettings.numIterations )
                return true;

            /************************************************************************/
            /* STEP 2: Choose a random input vector from the set of training data.                                                           
            /************************************************************************/
            int randomNum = SOM_Color_Form.util_randomInt( m_appSettings.inputVectors.Count );
            SOM_Color_Form.InputVector colorVec = (SOM_Color_Form.InputVector)m_appSettings.inputVectors[ randomNum ];

            /************************************************************************/
            /* STEP 3: Find the BMU.                                                           
            /************************************************************************/
            CNode bmu = findBMU( colorVec );

            /************************************************************************/
            /* STEP 4: Calculate the radius of the BMU's neighborhood.                                                           
            /************************************************************************/
            m_neighborhoodRadius = m_appSettings.mapRadius * (float)Math.Exp( -(float)m_currentIteration / m_timeConstant );

            /************************************************************************/
            /* STEP 5: Each neighboring node's weights are adjusted to make them more
             *         like the input vector.                                                       
            /************************************************************************/
            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                    float distToNodeSquared = 0.0f;

                    // Get the Euclidean distance (squared) to this node[i,j] from the BMU. Use
                    //  this formula to account for base 0 arrays, and the fact that our neighborhood
                    //  radius is actually HALF of the DRAWING WINDOW.
                    float bmuI = (float)(bmu.getI() + 1) * m_appSettings.nodeHeight;
                    float bmuJ = (float)(bmu.getJ() + 1) * m_appSettings.nodeWidth;
                    float nodeI = (float)(m_network[ i,j ].getI() + 1) * m_appSettings.nodeHeight;
                    float nodeJ = (float)(m_network[ i,j ].getJ() + 1) * m_appSettings.nodeWidth;
                    distToNodeSquared = (bmuI - nodeI) *
                                        (bmuI - nodeI) +
                                        (bmuJ - nodeJ) *
                                        (bmuJ - nodeJ);

                    float widthSquared = m_neighborhoodRadius * m_neighborhoodRadius;

                    // If within the neighborhood radius, adjust this nodes' weights.
                    if ( distToNodeSquared < widthSquared ) {
                        // Calculate how much it's weights are adjusted.
                        float influence = (float)Math.Exp( -distToNodeSquared / (2.0f * widthSquared) );

                        m_network[ i,j ].adjustWeights( colorVec, m_learningRate, influence );
                    }
                } // End for each column.
            } // End for each row.

            // Reduce the learning rate.
            m_learningRate = m_appSettings.startLearningRate * (float)Math.Exp( -(float)m_currentIteration / (float)m_appSettings.numIterations );
            ++m_currentIteration;

            return false;
        }

        private CNode findBMU( SOM_Color_Form.InputVector inputVec ) {
            CNode winner = null;

            float lowestDistance = 999999.0f;

            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                    float dist = m_network[ i,j ].calculateDistanceSquared( inputVec );

                    if ( dist < lowestDistance ) {
                        lowestDistance = dist;
                        winner = m_network[ i,j ];
                    }
                }
            }

            winner.incrementBMUCount();
            return winner;
        }

        /* Calculate BMU window once training is complete. Displays white node
         * on the nodes that were used as BMU's the most. # calculated = # of
         * input vectors.
         */
        private void windowsBMU() {
            // TODO: Find a better way to do this!
            ArrayList BMUs = new ArrayList();

            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                     BMUs.Add( m_network[ i,j ].getBMUCount() );
                }
            }

            BMUs.Sort();
            BMUs.Reverse();
            BMUs.RemoveRange( m_appSettings.inputVectors.Count, BMUs.Count - m_appSettings.inputVectors.Count );

            // Find the node's that these BMU's belong to...
            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                    for ( int k = 0; k < BMUs.Count; ++k ) {
                        if ( (int)BMUs[ k ] != 0 && m_network[ i,j ].getBMUCount() == (int)BMUs[ k ] ) {
                            for ( int w = 0; w < SOM_Color_Form.NUM_WEIGHTS; ++w ) {
                                m_window02[ i,j,w ] = 1.0f;
                            }

                            BMUs.RemoveAt( k );
                            break;
                        }
                    }
                } // End for each column.
            } // End for each row.
        }

        /* Calculate the Error Map window once training is completed. Return the total
         *  error of the map.
         * White/Light Greys = bad neighbors (weights very different).
         * Black = a good neighbors (weights similar).
         */
        private float windowsErrorMap() {
            
            // Sum of all the average errors for each node. Can be used to determine a relative
            //  effectiveness of a particular mapping.
            float totalError = 0.0f;
            bool takeSquareRoot = true;
            float numWeightSquareRoot = (float)Math.Sqrt( (double)SOM_Color_Form.NUM_WEIGHTS );

            // Find the average of all the node distances (add up the 8 surrounding nodes / 8).
            for ( int i = 0; i < SOM_Color_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Color_Form.NUM_NODES_ACROSS; ++j ) {
                    float sumDistance = 0.0f;
                    float[] centerPoint = m_network[ i,j ].getWeights();
                    int neighborCount = 0;

                    /*      i-1,j-1 | i-1,j | i-1,j+1 
                     *      -------------------------
                     *      i,j-1   |   X   | i,j+1
                     *      -------------------------
                     *      i+1,j-1 | i+1,j | i+1,j+1
                     */

                    // Top row.
                    if ( i >= 1 ) {
                        // Top left.
                        if ( j >= 1 ) {
                            sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i-1,j-1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }

                        // Top middle.
                        sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i-1,j ].getWeights(), takeSquareRoot );
                        ++neighborCount;

                        // Top right.
                        if ( j < SOM_Color_Form.NUM_NODES_ACROSS - 1 ) {
                            sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i-1,j+1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }
                    }
                    
                    // Left (1).
                    if ( j >= 1 ) {
                        sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i,j-1 ].getWeights(), takeSquareRoot );
                        ++neighborCount;
                    }

                    // Right (1).
                    if ( j < SOM_Color_Form.NUM_NODES_ACROSS - 1 ) {
                        sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i,j+1 ].getWeights(), takeSquareRoot );
                        ++neighborCount;
                    }

                    // Bottom row.
                    if ( i < SOM_Color_Form.NUM_NODES_DOWN - 1 ) {
                        // Bottom left.
                        if ( j >= 1 ) {
                            sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i+1,j-1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }

                        // Bottom middle.
                        sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i+1,j ].getWeights(), takeSquareRoot );
                        ++neighborCount;

                        // Bottom right.
                        if ( j < SOM_Color_Form.NUM_NODES_ACROSS - 1 ) {
                            sumDistance = SOM_Color_Form.util_getDistance( centerPoint, m_network[ i+1,j+1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }
                    }

                    // Compute the average.
                    float averageDistance = sumDistance / (float)neighborCount;
                    totalError += averageDistance;

                    // This produces a nice scale from 0 (black) to 1 (white) for the error map.
                    //   The max distance possible is when a node is 0.0, and all of it's neighbors
                    //   have 1.0 weights. This distance, then, would be NUM_WEIGHTS if no square root
                    //   is taken of the distances, or sqrt( NUM_WEIGHTS ) if the square root is taken.
                    //   The checks for out of bounds shouldn't be necessary, but are left in for extra
                    //   precaution.
                    float scaledDistance;
                    if ( takeSquareRoot ) { scaledDistance= numWeightSquareRoot * averageDistance; }
                    else                  { scaledDistance = (float)SOM_Color_Form.NUM_WEIGHTS * averageDistance; }
                    if ( scaledDistance > 1.0f ) {      scaledDistance = 1.0f;  }
                    else if ( scaledDistance < 0.0f ) { scaledDistance = 0.0f;  }

                    // Create a greyscale (i.e. r = g = b).
                    m_window03[ i,j,0 ] = m_window03[ i,j,1 ] = m_window03[ i,j,2 ] = scaledDistance;

                } // End for each column.
            } // End for each row.

            return totalError;
        }
    }
} // End namespace SOM_Color