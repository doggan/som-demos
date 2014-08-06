/*******************************************************************************/
/*  Author : Shyam M Guthikonda
/*  EMail  : shyamguth@gmail.com
/*  URL    : http://www.ShyamMichael.com
/*  Date   : 11 December 2005
/*  Desc.  : A self-organizing map (SOM) implementation for image recognition in C#.
/*           Given a database of images, this application will group them according
/*           to similarities in the network.  Ideas for the application were
/*           originally taken from generation5 @ 
/*           http://www.generation5.org/content/2004/aiSomPic.asp, although no code
/*           was provided by them. See the associated README for details on
/*           application usage.
/*******************************************************************************/

/* Additional Notes:
 * - All of the windows (rendered to) are assumed to be the same dimensions (square).
 *    Also, THEY MUST BE EVENLY DIVISIBLE by NUM_NODES_ACROSS and NUM_NODES_DOWN. If not,
 *    a round-off error will creep in (going from float to int) causing an odd grid-like
 *    pattern to appear in the windows.
 * - When adding new input vectors used to represent an image (i.e. histogram, area), make
 *    sure their values are NORMALIZED to 0.0 to 1.0 range, as the nodes in the network
 *    all are initialized to values in this range.
 * 
 * Todo:
 * - ImageList for displaying resultant pictures has hard-coded image display sizes (in the
 *    form designer).
 * - Currently only supports 24bpp bitmaps. (BGR)
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace SOM_Image {
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class SOM_Image_Form : System.Windows.Forms.Form {
        // Some constants that may change from app to app.
        public const int NUM_NODES_DOWN = 10;       // Should be changed as # of pics increase.
        public const int NUM_NODES_ACROSS = 10;     // Should be changed as # of pics increase.

        // The following should add up to NUM_WEIGHTS.. or else.
        public const int INPUT_VECTOR_SIZE_HISTOGRAM = 16;
        public const int INPUT_VECTOR_AREA_REGIONS_WIDE = 3;
        public const int INPUT_VECTOR_AREA_REGIONS_HIGH = 3;
        public const int INPUT_VECTOR_SIZE_AREA = (INPUT_VECTOR_AREA_REGIONS_WIDE * INPUT_VECTOR_AREA_REGIONS_HIGH) * 3;    // * 3 since RGB
        public const int NUM_WEIGHTS = INPUT_VECTOR_SIZE_HISTOGRAM + INPUT_VECTOR_SIZE_AREA;

        private System.Windows.Forms.StatusBar statusBar1;
        private System.Windows.Forms.RadioButton radioButton_Gradient;
        private System.Windows.Forms.RadioButton radioButton_Random;
        private System.Windows.Forms.GroupBox groupBox_Init;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_XMLFile;
        private System.Windows.Forms.Button button_XMLBrowse;
        private System.Windows.Forms.OpenFileDialog openFileDialog_XML;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button_Train;
        private System.Windows.Forms.Button button_Reset;
        private System.Windows.Forms.Label label_IfGreyClickReset;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button_DirectoryBrowse;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog_Directory;
        private System.Windows.Forms.TextBox textBox_ImageDirectory;
        private System.Windows.Forms.NumericUpDown numericUpDown_LearningRate_P1;
        private System.Windows.Forms.NumericUpDown numericUpDown_Iterations_P1;
        private System.Windows.Forms.NumericUpDown numericUpDown_Iterations_P2;
        private System.Windows.Forms.Panel panel_Network;
        private System.Windows.Forms.Panel panel_ErrorMap;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox checkBox_RecalculateDataVectors;
        private System.Windows.Forms.CheckBox checkBox_RecalculateBMUs;
        private System.Windows.Forms.ListView listView_ResultImages;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ImageList imageList_SelectedNode;
        private System.Windows.Forms.NumericUpDown numericUpDown_LearningRate_P2;

        /* A struct to hold the most recent application settings (updated
         * when the "Reset" button is clicked).
         */
        public class AppSettings {
            public INIT_FILL initFill;
            public float width, height;
            public float nodeWidth, nodeHeight;     // Used to represent nodes as colored squares.
            public float startLearningRate_P1, startLearningRate_P2;
            public int numIterations_P1, numIterations_P2;
            public ArrayList inputVectors;          // Contains InputVectors.
            public float mapRadius;
            public string imageDirectory;
            public bool isXMLProvided;      // TODO:
            public string XMLFileName;      // TODO:
            public bool recalculateDataVectors; // TODO:

            public ArrayList images;    // Used to store the image data each application run.

            public AppSettings() {
                inputVectors = new ArrayList();
                images = new ArrayList();
            }
        }

        /* The "useful" image data for one image, minus the raw data. This can be
         * calculated or taken from an XML file. The values can also be written to XML.
         */
        public class ImageData {
            public string m_fileName;
            public Point m_BMU;   // "Most recent" BMU index.   < 0 if no associated node.
            public float[] m_vectorHistogram;
            public float[] m_vectorArea;

            public ImageData() {
                m_vectorHistogram = new float[ INPUT_VECTOR_SIZE_HISTOGRAM ];
                m_vectorArea = new float[ INPUT_VECTOR_SIZE_AREA ];
                m_BMU.X = -1; m_BMU.Y = -1;
            }
        }

        /* How we should initialize all of our net nodes.
         */
        public enum INIT_FILL {
            random,
            gradient
        }

        public class InputVector {
            public float[] weights;

            public InputVector() {
                weights = new float[ SOM_Image_Form.NUM_WEIGHTS ];
            }

            public static InputVector operator +( InputVector v1, InputVector v2 ) {
                InputVector result = new InputVector();

                for ( int i = 0; i < SOM_Image_Form.NUM_WEIGHTS; ++i ) {
                    result.weights[ i ] = v1.weights[ i ] + v2.weights[ i ];
                }

                return result;
            }
        }

        private struct rgbColor {
            public float r, g, b;
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

        private const int NUM_WINDOWS = 2;
        private Graphics[] m_gWindow;
        private Graphics[] m_gOffscreenWindow;
        private Bitmap[] m_offscreenBitmap;
        private AppSettings m_appSettings;
        private CSOM m_SOM;
        private static Random m_random;
        private Timer m_timer;

        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuItem_File;
        private System.Windows.Forms.MenuItem menuItem_File_Quit;
        private System.Windows.Forms.MenuItem menuItem_Help;
        private System.Windows.Forms.MenuItem menuItem_Help_About;
        private System.Windows.Forms.Label label_LearningRate;
        private System.Windows.Forms.Label label_Iterations;
        private System.ComponentModel.IContainer components;

        public SOM_Image_Form() {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            listView_ResultImages.LargeImageList = imageList_SelectedNode;
            
            m_random = new Random();
            
            m_timer = new Timer();
            m_timer.Enabled = true;
            m_timer.Interval = 1;
            m_timer.Tick += new System.EventHandler( timer_tick );

            numericUpDown_LearningRate_P1.Value = (decimal).9;
            numericUpDown_LearningRate_P2.Value = (decimal).1;
            numericUpDown_Iterations_P1.Value = 500;
            numericUpDown_Iterations_P2.Value = 1000;

            /* 0 = Network Window (top)
             * 1 = Map Error Window (bottom)
             */
            m_gWindow = new Graphics[ NUM_WINDOWS ];
            m_gWindow[ 0 ] = panel_Network.CreateGraphics();
            m_gWindow[ 1 ] = panel_ErrorMap.CreateGraphics();

            m_offscreenBitmap = new Bitmap[ NUM_WINDOWS ];
            m_offscreenBitmap[ 0 ] = new Bitmap( panel_Network.Width, panel_Network.Height );
            m_offscreenBitmap[ 1 ] = new Bitmap( panel_ErrorMap.Width, panel_ErrorMap.Height );
            m_gOffscreenWindow = new Graphics[ NUM_WINDOWS ];
            m_gOffscreenWindow[ 0 ] = Graphics.FromImage( m_offscreenBitmap[ 0 ] );
            m_gOffscreenWindow[ 1 ] = Graphics.FromImage( m_offscreenBitmap[ 1 ] );

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
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SOM_Image_Form));
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.menuItem_File = new System.Windows.Forms.MenuItem();
            this.menuItem_File_Quit = new System.Windows.Forms.MenuItem();
            this.menuItem_Help = new System.Windows.Forms.MenuItem();
            this.menuItem_Help_About = new System.Windows.Forms.MenuItem();
            this.numericUpDown_LearningRate_P1 = new System.Windows.Forms.NumericUpDown();
            this.label_LearningRate = new System.Windows.Forms.Label();
            this.label_Iterations = new System.Windows.Forms.Label();
            this.numericUpDown_Iterations_P1 = new System.Windows.Forms.NumericUpDown();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.radioButton_Gradient = new System.Windows.Forms.RadioButton();
            this.radioButton_Random = new System.Windows.Forms.RadioButton();
            this.groupBox_Init = new System.Windows.Forms.GroupBox();
            this.numericUpDown_Iterations_P2 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_LearningRate_P2 = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.openFileDialog_XML = new System.Windows.Forms.OpenFileDialog();
            this.textBox_XMLFile = new System.Windows.Forms.TextBox();
            this.button_XMLBrowse = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.button_Train = new System.Windows.Forms.Button();
            this.button_Reset = new System.Windows.Forms.Button();
            this.label_IfGreyClickReset = new System.Windows.Forms.Label();
            this.textBox_ImageDirectory = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button_DirectoryBrowse = new System.Windows.Forms.Button();
            this.folderBrowserDialog_Directory = new System.Windows.Forms.FolderBrowserDialog();
            this.panel_Network = new System.Windows.Forms.Panel();
            this.panel_ErrorMap = new System.Windows.Forms.Panel();
            this.checkBox_RecalculateDataVectors = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBox_RecalculateBMUs = new System.Windows.Forms.CheckBox();
            this.listView_ResultImages = new System.Windows.Forms.ListView();
            this.label9 = new System.Windows.Forms.Label();
            this.imageList_SelectedNode = new System.Windows.Forms.ImageList(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LearningRate_P1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Iterations_P1)).BeginInit();
            this.groupBox_Init.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Iterations_P2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LearningRate_P2)).BeginInit();
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
            // numericUpDown_LearningRate_P1
            // 
            this.numericUpDown_LearningRate_P1.DecimalPlaces = 2;
            this.numericUpDown_LearningRate_P1.Increment = new System.Decimal(new int[] {
                                                                                            1,
                                                                                            0,
                                                                                            0,
                                                                                            131072});
            this.numericUpDown_LearningRate_P1.Location = new System.Drawing.Point(824, 384);
            this.numericUpDown_LearningRate_P1.Maximum = new System.Decimal(new int[] {
                                                                                          1,
                                                                                          0,
                                                                                          0,
                                                                                          0});
            this.numericUpDown_LearningRate_P1.Minimum = new System.Decimal(new int[] {
                                                                                          1,
                                                                                          0,
                                                                                          0,
                                                                                          131072});
            this.numericUpDown_LearningRate_P1.Name = "numericUpDown_LearningRate_P1";
            this.numericUpDown_LearningRate_P1.Size = new System.Drawing.Size(64, 20);
            this.numericUpDown_LearningRate_P1.TabIndex = 7;
            this.numericUpDown_LearningRate_P1.Value = new System.Decimal(new int[] {
                                                                                        1,
                                                                                        0,
                                                                                        0,
                                                                                        131072});
            this.numericUpDown_LearningRate_P1.ValueChanged += new System.EventHandler(this.numericUpDown_LearningRate_P1_ValueChanged);
            // 
            // label_LearningRate
            // 
            this.label_LearningRate.Location = new System.Drawing.Point(752, 368);
            this.label_LearningRate.Name = "label_LearningRate";
            this.label_LearningRate.Size = new System.Drawing.Size(80, 16);
            this.label_LearningRate.TabIndex = 8;
            this.label_LearningRate.Text = "Learning Rate:";
            // 
            // label_Iterations
            // 
            this.label_Iterations.Location = new System.Drawing.Point(752, 432);
            this.label_Iterations.Name = "label_Iterations";
            this.label_Iterations.Size = new System.Drawing.Size(56, 16);
            this.label_Iterations.TabIndex = 9;
            this.label_Iterations.Text = "Iterations:";
            // 
            // numericUpDown_Iterations_P1
            // 
            this.numericUpDown_Iterations_P1.Location = new System.Drawing.Point(824, 448);
            this.numericUpDown_Iterations_P1.Maximum = new System.Decimal(new int[] {
                                                                                        1000,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            this.numericUpDown_Iterations_P1.Minimum = new System.Decimal(new int[] {
                                                                                        1,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            this.numericUpDown_Iterations_P1.Name = "numericUpDown_Iterations_P1";
            this.numericUpDown_Iterations_P1.Size = new System.Drawing.Size(64, 20);
            this.numericUpDown_Iterations_P1.TabIndex = 10;
            this.numericUpDown_Iterations_P1.Value = new System.Decimal(new int[] {
                                                                                      1,
                                                                                      0,
                                                                                      0,
                                                                                      0});
            this.numericUpDown_Iterations_P1.ValueChanged += new System.EventHandler(this.numericUpDown_Iterations_P1_ValueChanged);
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 721);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Size = new System.Drawing.Size(894, 22);
            this.statusBar1.TabIndex = 14;
            this.statusBar1.Text = "Network Status: Neutral";
            // 
            // radioButton_Gradient
            // 
            this.radioButton_Gradient.Location = new System.Drawing.Point(88, 16);
            this.radioButton_Gradient.Name = "radioButton_Gradient";
            this.radioButton_Gradient.Size = new System.Drawing.Size(72, 24);
            this.radioButton_Gradient.TabIndex = 1;
            this.radioButton_Gradient.Text = "Gradient";
            this.radioButton_Gradient.CheckedChanged += new System.EventHandler(this.radioButton_Gradient_CheckedChanged);
            // 
            // radioButton_Random
            // 
            this.radioButton_Random.Checked = true;
            this.radioButton_Random.Location = new System.Drawing.Point(16, 16);
            this.radioButton_Random.Name = "radioButton_Random";
            this.radioButton_Random.Size = new System.Drawing.Size(72, 24);
            this.radioButton_Random.TabIndex = 0;
            this.radioButton_Random.TabStop = true;
            this.radioButton_Random.Text = "Random";
            this.radioButton_Random.CheckedChanged += new System.EventHandler(this.radioButton_Random_CheckedChanged);
            // 
            // groupBox_Init
            // 
            this.groupBox_Init.Controls.Add(this.radioButton_Gradient);
            this.groupBox_Init.Controls.Add(this.radioButton_Random);
            this.groupBox_Init.Location = new System.Drawing.Point(416, 8);
            this.groupBox_Init.Name = "groupBox_Init";
            this.groupBox_Init.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.groupBox_Init.Size = new System.Drawing.Size(168, 48);
            this.groupBox_Init.TabIndex = 0;
            this.groupBox_Init.TabStop = false;
            this.groupBox_Init.Text = "Initializations";
            // 
            // numericUpDown_Iterations_P2
            // 
            this.numericUpDown_Iterations_P2.Location = new System.Drawing.Point(824, 472);
            this.numericUpDown_Iterations_P2.Maximum = new System.Decimal(new int[] {
                                                                                        1000,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            this.numericUpDown_Iterations_P2.Minimum = new System.Decimal(new int[] {
                                                                                        1,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            this.numericUpDown_Iterations_P2.Name = "numericUpDown_Iterations_P2";
            this.numericUpDown_Iterations_P2.Size = new System.Drawing.Size(64, 20);
            this.numericUpDown_Iterations_P2.TabIndex = 16;
            this.numericUpDown_Iterations_P2.Value = new System.Decimal(new int[] {
                                                                                      1,
                                                                                      0,
                                                                                      0,
                                                                                      0});
            this.numericUpDown_Iterations_P2.ValueChanged += new System.EventHandler(this.numericUpDown_Iterations_P2_ValueChanged);
            // 
            // numericUpDown_LearningRate_P2
            // 
            this.numericUpDown_LearningRate_P2.DecimalPlaces = 2;
            this.numericUpDown_LearningRate_P2.Increment = new System.Decimal(new int[] {
                                                                                            1,
                                                                                            0,
                                                                                            0,
                                                                                            131072});
            this.numericUpDown_LearningRate_P2.Location = new System.Drawing.Point(824, 408);
            this.numericUpDown_LearningRate_P2.Maximum = new System.Decimal(new int[] {
                                                                                          1,
                                                                                          0,
                                                                                          0,
                                                                                          0});
            this.numericUpDown_LearningRate_P2.Minimum = new System.Decimal(new int[] {
                                                                                          1,
                                                                                          0,
                                                                                          0,
                                                                                          131072});
            this.numericUpDown_LearningRate_P2.Name = "numericUpDown_LearningRate_P2";
            this.numericUpDown_LearningRate_P2.Size = new System.Drawing.Size(64, 20);
            this.numericUpDown_LearningRate_P2.TabIndex = 17;
            this.numericUpDown_LearningRate_P2.Value = new System.Decimal(new int[] {
                                                                                        1,
                                                                                        0,
                                                                                        0,
                                                                                        131072});
            this.numericUpDown_LearningRate_P2.ValueChanged += new System.EventHandler(this.numericUpDown_LearningRate_P2_ValueChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(768, 384);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 16);
            this.label1.TabIndex = 18;
            this.label1.Text = "Phase 1:";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(768, 408);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 16);
            this.label2.TabIndex = 19;
            this.label2.Text = "Phase 2:";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(768, 448);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 16);
            this.label3.TabIndex = 20;
            this.label3.Text = "Phase 1:";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(768, 472);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 16);
            this.label4.TabIndex = 21;
            this.label4.Text = "Phase 2:";
            // 
            // openFileDialog_XML
            // 
            this.openFileDialog_XML.Filter = "XML Files (*.xml)|*.xml";
            // 
            // textBox_XMLFile
            // 
            this.textBox_XMLFile.Enabled = false;
            this.textBox_XMLFile.Location = new System.Drawing.Point(616, 568);
            this.textBox_XMLFile.Name = "textBox_XMLFile";
            this.textBox_XMLFile.ReadOnly = true;
            this.textBox_XMLFile.Size = new System.Drawing.Size(192, 20);
            this.textBox_XMLFile.TabIndex = 22;
            this.textBox_XMLFile.Text = "";
            // 
            // button_XMLBrowse
            // 
            this.button_XMLBrowse.Enabled = false;
            this.button_XMLBrowse.Location = new System.Drawing.Point(816, 568);
            this.button_XMLBrowse.Name = "button_XMLBrowse";
            this.button_XMLBrowse.Size = new System.Drawing.Size(56, 24);
            this.button_XMLBrowse.TabIndex = 23;
            this.button_XMLBrowse.Text = "Browse";
            this.button_XMLBrowse.Click += new System.EventHandler(this.clicked_button_XMLBrowse);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(616, 552);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(200, 16);
            this.label5.TabIndex = 24;
            this.label5.Text = "XML File (pre-calculated image data):";
            // 
            // button_Train
            // 
            this.button_Train.Enabled = false;
            this.button_Train.Location = new System.Drawing.Point(776, 688);
            this.button_Train.Name = "button_Train";
            this.button_Train.Size = new System.Drawing.Size(75, 24);
            this.button_Train.TabIndex = 6;
            this.button_Train.Text = "Train";
            this.button_Train.Click += new System.EventHandler(this.clicked_ButtonTrain);
            // 
            // button_Reset
            // 
            this.button_Reset.Location = new System.Drawing.Point(680, 688);
            this.button_Reset.Name = "button_Reset";
            this.button_Reset.Size = new System.Drawing.Size(75, 24);
            this.button_Reset.TabIndex = 5;
            this.button_Reset.Text = "Reset";
            this.button_Reset.Click += new System.EventHandler(this.clicked_ButtonReset);
            // 
            // label_IfGreyClickReset
            // 
            this.label_IfGreyClickReset.Location = new System.Drawing.Point(760, 664);
            this.label_IfGreyClickReset.Name = "label_IfGreyClickReset";
            this.label_IfGreyClickReset.Size = new System.Drawing.Size(112, 16);
            this.label_IfGreyClickReset.TabIndex = 15;
            this.label_IfGreyClickReset.Text = "(If grey, click Reset).";
            // 
            // textBox_ImageDirectory
            // 
            this.textBox_ImageDirectory.Location = new System.Drawing.Point(616, 520);
            this.textBox_ImageDirectory.Name = "textBox_ImageDirectory";
            this.textBox_ImageDirectory.ReadOnly = true;
            this.textBox_ImageDirectory.Size = new System.Drawing.Size(192, 20);
            this.textBox_ImageDirectory.TabIndex = 25;
            this.textBox_ImageDirectory.Text = "";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(616, 504);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 16);
            this.label6.TabIndex = 26;
            this.label6.Text = "Image Directory:";
            // 
            // button_DirectoryBrowse
            // 
            this.button_DirectoryBrowse.Location = new System.Drawing.Point(816, 520);
            this.button_DirectoryBrowse.Name = "button_DirectoryBrowse";
            this.button_DirectoryBrowse.Size = new System.Drawing.Size(56, 24);
            this.button_DirectoryBrowse.TabIndex = 27;
            this.button_DirectoryBrowse.Text = "Browse";
            this.button_DirectoryBrowse.Click += new System.EventHandler(this.clicked_button_DirectoryBrowse);
            // 
            // panel_Network
            // 
            this.panel_Network.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.panel_Network.Location = new System.Drawing.Point(296, 72);
            this.panel_Network.Name = "panel_Network";
            this.panel_Network.Size = new System.Drawing.Size(300, 300);
            this.panel_Network.TabIndex = 28;
            this.panel_Network.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mouseDown_Network);
            // 
            // panel_ErrorMap
            // 
            this.panel_ErrorMap.BackColor = System.Drawing.SystemColors.Highlight;
            this.panel_ErrorMap.Location = new System.Drawing.Point(296, 400);
            this.panel_ErrorMap.Name = "panel_ErrorMap";
            this.panel_ErrorMap.Size = new System.Drawing.Size(300, 300);
            this.panel_ErrorMap.TabIndex = 29;
            // 
            // checkBox_RecalculateDataVectors
            // 
            this.checkBox_RecalculateDataVectors.Enabled = false;
            this.checkBox_RecalculateDataVectors.Location = new System.Drawing.Point(648, 592);
            this.checkBox_RecalculateDataVectors.Name = "checkBox_RecalculateDataVectors";
            this.checkBox_RecalculateDataVectors.Size = new System.Drawing.Size(160, 32);
            this.checkBox_RecalculateDataVectors.TabIndex = 30;
            this.checkBox_RecalculateDataVectors.Text = "Recalculate Data Vectors?";
            this.checkBox_RecalculateDataVectors.Click += new System.EventHandler(this.checkBox_RecalculateData_CheckChanged);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(296, 56);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 16);
            this.label7.TabIndex = 31;
            this.label7.Text = "Network:";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(296, 384);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 16);
            this.label8.TabIndex = 32;
            this.label8.Text = "Error Map:";
            // 
            // checkBox_RecalculateBMUs
            // 
            this.checkBox_RecalculateBMUs.Enabled = false;
            this.checkBox_RecalculateBMUs.Location = new System.Drawing.Point(648, 624);
            this.checkBox_RecalculateBMUs.Name = "checkBox_RecalculateBMUs";
            this.checkBox_RecalculateBMUs.Size = new System.Drawing.Size(152, 16);
            this.checkBox_RecalculateBMUs.TabIndex = 33;
            this.checkBox_RecalculateBMUs.Text = "Recalculate XML BMUs?";
            this.checkBox_RecalculateBMUs.Click += new System.EventHandler(this.checkBox_RecalculateXMLBMU_CheckChanged);
            // 
            // listView_ResultImages
            // 
            this.listView_ResultImages.Location = new System.Drawing.Point(8, 72);
            this.listView_ResultImages.Name = "listView_ResultImages";
            this.listView_ResultImages.Size = new System.Drawing.Size(275, 480);
            this.listView_ResultImages.TabIndex = 34;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(8, 56);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(136, 16);
            this.label9.TabIndex = 35;
            this.label9.Text = "Images in Selected Node:";
            // 
            // imageList_SelectedNode
            // 
            this.imageList_SelectedNode.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            this.imageList_SelectedNode.ImageSize = new System.Drawing.Size(256, 192);
            this.imageList_SelectedNode.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // SOM_Image_Form
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(894, 743);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.listView_ResultImages);
            this.Controls.Add(this.checkBox_RecalculateBMUs);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.checkBox_RecalculateDataVectors);
            this.Controls.Add(this.panel_ErrorMap);
            this.Controls.Add(this.panel_Network);
            this.Controls.Add(this.button_DirectoryBrowse);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_ImageDirectory);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button_XMLBrowse);
            this.Controls.Add(this.textBox_XMLFile);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDown_LearningRate_P2);
            this.Controls.Add(this.numericUpDown_Iterations_P2);
            this.Controls.Add(this.label_IfGreyClickReset);
            this.Controls.Add(this.statusBar1);
            this.Controls.Add(this.numericUpDown_Iterations_P1);
            this.Controls.Add(this.label_Iterations);
            this.Controls.Add(this.label_LearningRate);
            this.Controls.Add(this.numericUpDown_LearningRate_P1);
            this.Controls.Add(this.button_Train);
            this.Controls.Add(this.button_Reset);
            this.Controls.Add(this.groupBox_Init);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Menu = this.mainMenu1;
            this.Name = "SOM_Image_Form";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Text = "SOM_Image";
            this.Load += new System.EventHandler(this.SOM_Color_Form_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LearningRate_P1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Iterations_P1)).EndInit();
            this.groupBox_Init.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Iterations_P2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LearningRate_P2)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.Run(new SOM_Image_Form());
        }

        private void SOM_Color_Form_Load(object sender, System.EventArgs e) {
        
        }

        private void timer_tick( object sender, System.EventArgs e ) {
            Invalidate();
        }

        private void getInputVectors() {
//            // TODO: get rid of this! For testing convenience only.
//            textBox_ImageDirectory.Text = "C:\\Documents and Settings\\shyam\\My Documents\\My Pictures\\Captures\\";
//            m_appSettings.imageDirectory = textBox_ImageDirectory.Text;
//            /////////////////////////////

            if ( m_appSettings.imageDirectory == "" )
                return;

            if ( !m_appSettings.recalculateDataVectors )
                return;

            m_appSettings.inputVectors.Clear();
            m_appSettings.images.Clear();
      
            System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo( m_appSettings.imageDirectory );
            System.IO.FileInfo[] fileInfo = directoryInfo.GetFiles( "*.bmp" );
      
            // For each image file, in the currently selected directory, with the appropriate extension.
            for ( int imageNumber = 0; imageNumber < fileInfo.Length; ++imageNumber ) {
                string thisFileName = fileInfo[ imageNumber ].ToString();

                Bitmap img = new Bitmap( m_appSettings.imageDirectory + thisFileName);
                BitmapData imgData = img.LockBits( new Rectangle( 0, 0, img.Width, img.Height ), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb );

                rgbColor[,] imgArray = new rgbColor[ imgData.Height, imgData.Width ];

                unsafe {
                    byte *imgPtr = (byte*)(imgData.Scan0);

                    for ( int i = 0; i < imgData.Height; ++i ) {
                        for ( int j = 0; j < imgData.Width; ++j ) {
                            imgArray[ i,j ].b = (float)*imgPtr;
                            ++imgPtr;
                            imgArray[ i,j ].g = (int)*imgPtr;
                            ++imgPtr;
                            imgArray[ i,j ].r = (int)*imgPtr;
                            ++imgPtr;
                        }
                        imgPtr += imgData.Stride - imgData.Width * 3;
                    }
                } // End unsafe code.

                // Needed or else RELEASE build will crash! eep!
                img.UnlockBits( imgData );

                // Compute the "input vectors" for this image, then stuff all "input vectors" into a single 
                //  input vector of length SOM_Image_Form.NUM_WEIGHTS.
                InputVector inVec = new InputVector();
                ImageData anImage = new ImageData();

                anImage.m_fileName = thisFileName;
                
                float[] histogramVector = anImage.m_vectorHistogram = calculateInputVector_Histogram( imgArray, imgData.Height, imgData.Width );
                float[] areaVector = anImage.m_vectorArea = calculateInputVector_Area( imgArray, imgData.Height, imgData.Width );
                System.Diagnostics.Debug.Assert( histogramVector.Length + areaVector.Length == SOM_Image_Form.NUM_WEIGHTS );

                // Pack all vectors into the single vector.
                for ( int i = 0, j = 0; i < SOM_Image_Form.NUM_WEIGHTS; ++i ) {
                    if ( i < histogramVector.Length ) {
                        inVec.weights[ i ] = histogramVector[ i ];
                    }
                    else if ( j < areaVector.Length ) {
                        inVec.weights[ i ] = areaVector[ j ];
                        ++j;
                    }
                }

                m_appSettings.inputVectors.Add( inVec );
                m_appSettings.images.Add( anImage );
            } // End for each image in this directory.
        }

        /* This vector represents the lights/darks of an image. Convert each pixel to gray-
         * scale (0-255). Create X bins, and divide each pixel into the bin with like-pixels.
         * Increment the count (numPixelsInBin) of each bin every time a pixel is added. Normalize
         * this value, dividing by the total # of pixels. This gives a unique X element vector
         * for this image describing its' lights/darks.
         */
        private float[] calculateInputVector_Histogram( rgbColor[,] imgArray, int imgHeight, int imgWidth ) {
            float[] grayArray = new float[ imgArray.Length ];
            float maxNumInBucket = (float)(imgHeight * imgWidth);     // Used to normalize this vector.

            // Convert each pixel to a single gray-scale value.
            for ( int i = 0; i < imgHeight; ++i ) {
                for ( int j = 0; j < imgWidth; ++j ) {
                    grayArray[ (i * imgWidth) + j ] = (imgArray[ i,j ].r * .333333f) +
                                                      (imgArray[ i,j ].g * .333333f) +
                                                      (imgArray[ i,j ].b * .333333f);
                }
            }

            int numberOfBins = INPUT_VECTOR_SIZE_HISTOGRAM;
            float[] resultArray = new float[ numberOfBins ];

            for ( int i = 0; i < grayArray.Length; ++i ) {
                int binNumber = (int)grayArray[ i ] / numberOfBins;
                ++resultArray[ binNumber ];
            }

            // Normalize.
            for ( int i = 0; i < resultArray.Length; ++i ) {
                resultArray[ i ] /= maxNumInBucket;
            }

            return resultArray;
        }

        /* Chop the image into X regions. Calculate the average RGB values for each region (1
         * RGB value per region). This gives you X * 3 values describing the color arrangement
         * of this image.
         */
        private float[] calculateInputVector_Area( rgbColor[,] imgArray, int imgHeight, int imgWidth ) {
            float[] result = new float[ INPUT_VECTOR_SIZE_AREA ];

            int pixPerRegionHigh = imgHeight / INPUT_VECTOR_AREA_REGIONS_HIGH;
            int pixPerRegionWide = imgWidth / INPUT_VECTOR_AREA_REGIONS_WIDE;

            /* A 9 region image is traversed as such:
             * 
             *      0 | 1 | 2
             *      ---------
             *      3 | 4 | 5
             *      ---------
             *      6 | 7 | 8
             */

            // Traverse through all regions.
            for ( int rHigh = 0; rHigh < INPUT_VECTOR_AREA_REGIONS_HIGH; ++rHigh ) {
                for ( int rWide = 0; rWide < INPUT_VECTOR_AREA_REGIONS_WIDE; ++rWide ) {

                    float totalPixels = 0.0f;
                    SOM_Image_Form.rgbColor totalColor;
                    totalColor.r = totalColor.g = totalColor.b = 0.0f;

                    for ( int i = rHigh * pixPerRegionHigh; i < (rHigh + 1) * pixPerRegionHigh; ++i ) {
                        for ( int j = rWide * pixPerRegionWide; j < (rWide + 1) * pixPerRegionWide; ++j ) {

                            totalColor.r += imgArray[ i,j ].r;
                            totalColor.g += imgArray[ i,j ].g;
                            totalColor.b += imgArray[ i,j ].b;
                            ++totalPixels;

                        } // End for each pixel across.
                    } // End for each pixel down.

                    // Calculate the average.
                    totalColor.r /= totalPixels;
                    totalColor.g /= totalPixels;
                    totalColor.b /= totalPixels;

                    // Store the averages (separate rgb values).
                    result[ 3 * ((rHigh * SOM_Image_Form.INPUT_VECTOR_AREA_REGIONS_WIDE) + rWide) ] = totalColor.r;
                    result[ 3 * ((rHigh * SOM_Image_Form.INPUT_VECTOR_AREA_REGIONS_WIDE) + rWide) + 1 ] = totalColor.g;
                    result[ 3 * ((rHigh * SOM_Image_Form.INPUT_VECTOR_AREA_REGIONS_WIDE) + rWide) + 2 ] = totalColor.b;

                } // End for each region across.
            } // End for each region down.

            // Normalize to 0.0 to 1.0 range;
            for ( int i = 0; i < result.Length; ++i ) {
                result[ i ] /= 255.0f;
            }

            return result;
        }

        /* This method is called on app initialization and whenever the RESET
         * button is clicked. It will fill out the AppSettings struct with the
         * current settings.
         */
        private void updateAppSettings() {
            // Initialization fill.
            if ( radioButton_Random.Checked )
                m_appSettings.initFill = INIT_FILL.random;
            else if ( radioButton_Gradient.Checked )
                m_appSettings.initFill = INIT_FILL.gradient;

            // Assumes both rendering windows are the same dimensions.
            Size s = panel_Network.Size;
            m_appSettings.width = (float)s.Width;
            m_appSettings.height = (float)s.Height;

            // Calculate node square dimensions.
            m_appSettings.nodeWidth = m_appSettings.width / (float)SOM_Image_Form.NUM_NODES_ACROSS;
            m_appSettings.nodeHeight = m_appSettings.height / (float)SOM_Image_Form.NUM_NODES_DOWN;

            m_appSettings.mapRadius = Math.Max( m_appSettings.width, m_appSettings.height ) / 2.0f;

            m_appSettings.startLearningRate_P1 = (float)Decimal.ToDouble( numericUpDown_LearningRate_P1.Value );
            m_appSettings.startLearningRate_P2 = (float)Decimal.ToDouble( numericUpDown_LearningRate_P2.Value );

            m_appSettings.numIterations_P1 = Decimal.ToInt32( numericUpDown_Iterations_P1.Value );
            m_appSettings.numIterations_P2 = Decimal.ToInt32( numericUpDown_Iterations_P2.Value );

            // XML file provided?
            if ( textBox_XMLFile.Text != "" ) {
                // We can only not recalculate if we have been supplied the values through an XML file.
                if ( !checkBox_RecalculateDataVectors.Checked )  m_appSettings.recalculateDataVectors = false;

                m_appSettings.isXMLProvided = true;
                m_appSettings.XMLFileName = textBox_XMLFile.Text;
            }
            else {
                m_appSettings.recalculateDataVectors = true;
                m_appSettings.isXMLProvided = false;
                m_appSettings.XMLFileName = "";
            }

            m_appSettings.imageDirectory = textBox_ImageDirectory.Text;

            getInputVectors();
        }

        protected override void OnPaint(PaintEventArgs pe) {
            if ( m_SOM == null )
                return;

            // Update the status bar.
            float tme = m_SOM.getTotalMapError();
            if ( tme <= 0.0f )  this.statusBar1.Text = m_SOM.getNetState() + "... Total Map Error: -";
            else                this.statusBar1.Text = m_SOM.getNetState() + "... Total Map Error: " + tme.ToString();

            m_SOM.render( m_gWindow, m_gOffscreenWindow, m_offscreenBitmap );
        }

        private void menu_File_Quit(object sender, System.EventArgs e) {
            Application.Exit();
        }

        private void menu_Help_About(object sender, System.EventArgs e) {
            SOM_Image.form_About a = new form_About();
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
            if ( m_appSettings.imageDirectory == "" ) {
                MessageBox.Show( "Please select a directory to read images from." );
                return;
            }

            if ( m_SOM.isAlreadyTrained() ) {
                MessageBox.Show( "SOM already trained. Hit RESET then TRAIN to re-train." );
                return;
            }

            if ( !m_SOM.isTraining() )
                m_SOM.startTraining();
        }

        /* If a value is changed, user must click reset before clicking train.
         */
        private void mustReset() {
            button_Train.Enabled = false;
        }

        private void numericUpDown_LearningRate_P1_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void numericUpDown_LearningRate_P2_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void numericUpDown_Iterations_P1_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void numericUpDown_Iterations_P2_ValueChanged(object sender, System.EventArgs e) { mustReset(); }
        private void radioButton_Random_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
        private void radioButton_Gradient_CheckedChanged(object sender, System.EventArgs e) { mustReset(); }
        private void checkBox_RecalculateData_CheckChanged(object sender, System.EventArgs e) { mustReset(); }
        private void checkBox_RecalculateXMLBMU_CheckChanged(object sender, System.EventArgs e) { mustReset(); }

        private void clicked_button_XMLBrowse(object sender, System.EventArgs e) {
            if ( openFileDialog_XML.ShowDialog() == DialogResult.OK ) {
                textBox_XMLFile.Text = openFileDialog_XML.FileName;
                mustReset();
            }
        }

        private void clicked_button_DirectoryBrowse(object sender, System.EventArgs e) {
            if ( folderBrowserDialog_Directory.ShowDialog() == DialogResult.OK ) {
                textBox_ImageDirectory.Text = folderBrowserDialog_Directory.SelectedPath;
                if ( !textBox_ImageDirectory.Text.EndsWith( "\\" ) )
                    textBox_ImageDirectory.Text += "\\";
                mustReset();
            }
        }

        /* Determines where the mouse has been clicked in the network grid so that we
         * can determine the node to show its' associated images.
         */
        private void mouseDown_Network(object sender, System.Windows.Forms.MouseEventArgs e) {
            if ( e.Button == MouseButtons.Left ) {
                
                imageList_SelectedNode.Images.Clear();
                listView_ResultImages.Items.Clear();

                if ( m_SOM != null ) {
                    // Don't process clicks while the SOM is learning.
                    if ( !m_SOM.isTraining() ) {
                        string[] fileNames = m_SOM.clickInGrid( e.X, e.Y );
                        mustReset();

                        int uniqueIndex = 0;
                        foreach ( string i in fileNames ) {
                            imageList_SelectedNode.Images.Add( System.Drawing.Image.FromFile( m_appSettings.imageDirectory + i ) );
                            listView_ResultImages.Items.Add( i, uniqueIndex++ );
                        }
                    } // End if not training.
                } // End if SOM exists.
            } // End if LMB.
        }
    }

    /* Node class. Used to represent a single weighted node in the network.
     */
    public class CNode {
        private int m_i, m_j;
        private float[] m_weights;
        private ArrayList m_imageNames;

        public CNode( int i, int j ) {
            m_i = i; m_j = j;
            m_weights = new float[ SOM_Image_Form.NUM_WEIGHTS ];
            m_imageNames = new ArrayList();
        }

        public void setWeights( float [] weight ) {
            for ( int i = 0; i < weight.Length; ++i )
                m_weights[ i ] = weight[ i ];
        }

        public float[] getWeights() { return m_weights; }

        public int getI() { return m_i; }
        public int getJ() { return m_j; }

        public void addImage( string anImageName ) {
            m_imageNames.Add( anImageName );
        }
        public int getNumImages() {
            return m_imageNames.Count;
        }
        public string[] getImageFileNames() {
            string[] result = new string[ m_imageNames.Count ];

            for ( int i = 0; i < result.Length; ++i ) {
                result[ i ] = (string)m_imageNames[ i ];
            }

            return result;
        }

        public float calculateDistanceSquared( SOM_Image_Form.InputVector inputVec ) {
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
        public void adjustWeights( SOM_Image_Form.InputVector targetVector, float learningRate, float influence ) {
            for ( int w = 0; w < targetVector.weights.Length; ++w ) {
                m_weights[ w ] += learningRate * influence * (targetVector.weights[ w ] - m_weights[ w ]);
            }
        }
    }

    /* SOM class. Encapsulates all SOM logic.
     */
    public class CSOM {
        private SOM_Image_Form.AppSettings m_appSettings;
        private CNode[,] m_network;
        private NET_STATE m_netState;
        private TRAINING_PHASE m_trainingPhase;
        private int m_currentIteration;
        private float m_neighborhoodRadius;
        private float m_timeConstant_P1;
        private float m_learningRate_P1, m_learningRate_P2;
        private float m_totalMapError;
        private bool m_isNodeSelected;
        private Point m_selectedNodeCoords;
        private bool m_isAlreadyTrained;

        /*  Used to hold color values for the windows (window #0 stores it's
         *  values as the weight of each node in the network).
         *
         *  m_window01 = Error map window.
         */
        private float[,,] m_window01;

        private enum NET_STATE {
            neutral,    // Nothing needs to be done. Waiting.
            init,       // We need to initialize all the windows.
            training,   // Need to update (window 2) to show progress.
            finished    // Display the results (calculate windows 3 and 4).
        }

        private enum TRAINING_PHASE {
            phase_1,
            phase_2
        }

        public bool isAlreadyTrained() { return m_isAlreadyTrained; }

        public float getTotalMapError() { return m_totalMapError; }

        public string getNetState() {
            if ( m_netState == NET_STATE.finished ) {
                return "Network Status: Finished...";
            }
            else if ( m_netState == NET_STATE.init ) {
                return "Network Status: Initializing...";
            }
            else if ( m_netState == NET_STATE.training ) {
                if ( m_trainingPhase == TRAINING_PHASE.phase_1 )
                    return "Network Status: Training... Phase 1: Iteration #" + (m_currentIteration - 1).ToString();
                else
                    return "Network Status: Training... Phase 2: Iteration #" + (m_currentIteration - 1).ToString();
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

        public CSOM( SOM_Image_Form.AppSettings appSettings ) {
            m_appSettings = appSettings;
            m_netState = NET_STATE.init;
            m_trainingPhase = TRAINING_PHASE.phase_1;
            m_currentIteration = 1;
            m_totalMapError = 0.0f;
            m_isNodeSelected = false;
            m_selectedNodeCoords = new Point( 0, 0 );
            m_isAlreadyTrained = false;

            m_timeConstant_P1 = (float)m_appSettings.numIterations_P1 / (float)Math.Log( m_appSettings.mapRadius );
            m_learningRate_P1 = m_appSettings.startLearningRate_P1;
            m_learningRate_P2 = m_appSettings.startLearningRate_P2;

            // Allocate memory.
            m_network = new CNode[ SOM_Image_Form.NUM_NODES_DOWN, SOM_Image_Form.NUM_NODES_ACROSS ];
            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {
                    m_network[ i,j ] = new CNode( i, j );
                }
            }
            m_window01 = new float[ SOM_Image_Form.NUM_NODES_DOWN, SOM_Image_Form.NUM_NODES_ACROSS, 3 ];

            calcWindows();
        }

        /* Renders all windows of the application.
         */
        public void render( Graphics[] theWindows, Graphics[] offscreenWindows, Bitmap[] offscreenBitmaps ) {
            calcWindows();

            // Manually 'blit' to background color.
            SolidBrush grayBrush = new SolidBrush( Color.LightGray );
            Rectangle fullRect = new Rectangle( 0, 0, (int)m_appSettings.width, (int)m_appSettings.height );
            offscreenWindows[ 0 ].FillRectangle( grayBrush, fullRect );

            // Network window "grid" code:
            Pen pen0;
            pen0 = new Pen( Color.Red, 1.0f );

            // Calculate Vertical Lines.
            Point[] pTop = new Point[ SOM_Image_Form.NUM_NODES_ACROSS - 1 ];
            Point[] pBottom = new Point[ SOM_Image_Form.NUM_NODES_ACROSS - 1 ];
            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_ACROSS - 1; ++i ) {
                pTop[ i ] = new Point( (i+1) * (int)m_appSettings.nodeWidth - 1, 0 );
                pBottom[ i ] = new Point( (i+1) * (int)m_appSettings.nodeWidth - 1, (int)m_appSettings.height );
            }

            // Calculate Horizontal Lines.
            Point[] pLeft = new Point[ SOM_Image_Form.NUM_NODES_DOWN - 1 ];
            Point[] pRight = new Point[ SOM_Image_Form.NUM_NODES_DOWN - 1 ];
            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN - 1; ++i ) {
                pLeft[ i ] = new Point( 0, (i+1) * (int)m_appSettings.nodeHeight );
                pRight[ i ] = new Point( (int)m_appSettings.width, (i+1) * (int)m_appSettings.nodeHeight );
            }

            // Render grid.
            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_ACROSS - 1; ++i ) {
                offscreenWindows[ 0 ].DrawLine( pen0, pTop[ i ], pBottom[ i ] );
                offscreenWindows[ 0 ].DrawLine( pen0, pLeft[ i ], pRight[ i ] );
            }

            Font f = new Font( "Times New Roman", 10.0f );
            SolidBrush blackBrush = new SolidBrush( Color.Black );
            SolidBrush whiteBrush = new SolidBrush( Color.WhiteSmoke );

            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {

                    // Render any selected node, then all of the grid #s (of images) on top.
                    if ( m_isNodeSelected ) {
                        if ( m_selectedNodeCoords.X == i && m_selectedNodeCoords.Y == j ) {

                            // Beware: Ugly code ahead. (Accounts for top row and right column being 1 pixel bigger than other cells).
                            int hackX = 1; int hackY = 1;
                            if ( i == SOM_Image_Form.NUM_NODES_DOWN - 1 ) hackX = 0;
                            if ( j == 0 ) hackY = 0;
                            ////////////////////////////////
                            
                            Point pSelected = new Point( (int)(m_appSettings.nodeWidth * i), (int)(m_appSettings.nodeHeight * j) + hackY );
                            Size sSelected = new Size( (int)m_appSettings.nodeWidth - hackX, (int)m_appSettings.nodeHeight - hackY );
                            Rectangle selectedRect = new Rectangle( pSelected, sSelected );
                            offscreenWindows[ 0 ].FillRectangle( whiteBrush, selectedRect );
                        }
                    }

                    Point pFont = new Point( (i+1)*(int)m_appSettings.nodeWidth - (int)(.67*m_appSettings.nodeWidth),
                                             (j+1)*(int)m_appSettings.nodeHeight - (int)(.7*m_appSettings.nodeHeight));
                    offscreenWindows[ 0 ].DrawString( m_network[ i,j ].getNumImages().ToString(), f, blackBrush, pFont );


                    // Deal with the other windows.
                    float[] weights1 = new float[ 3 ];

                    for ( int w = 0; w < 3; ++w ) {
                        weights1[ w ] = m_window01[ i,j,w ];
                    }

                    Rectangle rect = new Rectangle( 
                        (int)(m_appSettings.nodeWidth * i), (int)(m_appSettings.nodeHeight * j),
                        (int)m_appSettings.nodeWidth, (int)m_appSettings.nodeHeight
                        );

                    SolidBrush brush1;
                    brush1 = new SolidBrush( Color.FromArgb(
                        SOM_Image_Form.util_floatToByte( weights1[ 0 ] ), 
                        SOM_Image_Form.util_floatToByte( weights1[ 1 ] ), 
                        SOM_Image_Form.util_floatToByte( weights1[ 2 ] ) )
                        );

                    // Render error map.
                    offscreenWindows[ 1 ].FillRectangle( brush1, rect );
                } // End for each column.
            } // End for each row.

            theWindows[ 0 ].DrawImage( offscreenBitmaps[ 0 ], 0, 0 );
            theWindows[ 1 ].DrawImage( offscreenBitmaps[ 1 ], 0, 0 );
        }

        /* When the user clicks in the network grid, this function will determine
         * the node clicked on, and perform the appropriate actions. If the network
         * has already been trained and the node clicked on has images associated with it,
         * this function will return an array of those file names so the main application
         * class can decide what to do with that information.
         */
        public string[] clickInGrid( int xCoord, int yCoord ) {
            // Yay integer division to the rescue!
            m_selectedNodeCoords.X = xCoord / (int)m_appSettings.nodeWidth;
            m_selectedNodeCoords.Y = yCoord / (int)m_appSettings.nodeHeight;
            m_isNodeSelected = true;

            string[] imageFileNames = m_network[ m_selectedNodeCoords.X, m_selectedNodeCoords.Y ].getImageFileNames();

            return imageFileNames;
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
                    m_isAlreadyTrained = false;
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
                    m_isAlreadyTrained = true;
                    m_totalMapError = windowsErrorMap();
                    associateImageWithNode();

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
            float[] weight = new float[ SOM_Image_Form.NUM_WEIGHTS ];

            /************************************************************************/
            /* STEP 1: Each nodes' weights are initialized.                                                           
            /************************************************************************/
            switch ( m_appSettings.initFill ) {
                case SOM_Image_Form.INIT_FILL.random: {
                    // Initialize all weights to random floats (0.0 to 1.0).
                    for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                        for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {
                            CNode currentNode = m_network[ i,j ];
                            
                            for ( int w = 0; w < SOM_Image_Form.NUM_WEIGHTS; ++w ) {
                                weight[ w ] = SOM_Image_Form.util_randomFloatOneToZero();
                                if ( w < 3 )
                                    m_window01[ i,j,w ] = 0.0f; // Black.
                            }
                            currentNode.setWeights( weight );
                        } // End for each column.
                    } // End for each row.
                    break;
                }
                case SOM_Image_Form.INIT_FILL.gradient: {
                    for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                        for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {
                            CNode currentNode = m_network[ i,j ];

                            float gradVal = (float)(i + j) / (float)(SOM_Image_Form.NUM_NODES_DOWN + SOM_Image_Form.NUM_NODES_ACROSS - 2);
                            for ( int w = 0; w < SOM_Image_Form.NUM_WEIGHTS; ++w ) {
                                weight[ w ] = gradVal;
                                if ( w < 3 )
                                    m_window01[ i,j,w ] = 0.0f; // Black.
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
            if ( m_trainingPhase == TRAINING_PHASE.phase_1 && m_currentIteration > m_appSettings.numIterations_P1 ) {
                m_trainingPhase = TRAINING_PHASE.phase_2;
                m_currentIteration = 1;
            }
            else if ( m_trainingPhase == TRAINING_PHASE.phase_2 && m_currentIteration > m_appSettings.numIterations_P2 ) {
                return true;
            }

            /************************************************************************/
            /* STEP 2: Choose a random input vector from the set of training data.                                                           
            /************************************************************************/
            int randomNum = SOM_Image_Form.util_randomInt( m_appSettings.inputVectors.Count );
            SOM_Image_Form.InputVector inVec = (SOM_Image_Form.InputVector)m_appSettings.inputVectors[ randomNum ];
            SOM_Image_Form.ImageData thisImage = (SOM_Image_Form.ImageData)m_appSettings.images[ randomNum ];

            /************************************************************************/
            /* STEP 3: Find the BMU.                                                           
            /************************************************************************/
            CNode bmu = findBMU( inVec );

            // Update this image's most recent BMU for later identification.
            thisImage.m_BMU.X = bmu.getI();
            thisImage.m_BMU.Y = bmu.getJ();

            /************************************************************************/
            /* STEP 4: Calculate the radius of the BMU's neighborhood.                                                           
            /************************************************************************/
            if ( m_trainingPhase == TRAINING_PHASE.phase_1 )
                m_neighborhoodRadius = m_appSettings.mapRadius * (float)Math.Exp( -(float)m_currentIteration / m_timeConstant_P1 );
            else if ( m_trainingPhase == TRAINING_PHASE.phase_2 )
                m_neighborhoodRadius = 1.0f;

            /************************************************************************/
            /* STEP 5: Each neighboring node's weights are adjusted to make them more
             *         like the input vector.                                                       
            /************************************************************************/
            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {
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

                        if ( m_trainingPhase == TRAINING_PHASE.phase_1 )
                            m_network[ i,j ].adjustWeights( inVec, m_learningRate_P1, influence );
                        else if ( m_trainingPhase == TRAINING_PHASE.phase_2 )
                            m_network[ i,j ].adjustWeights( inVec, m_learningRate_P2, influence );
                    }
                } // End for each column.
            } // End for each row.

            // Reduce the learning rate.
            if ( m_trainingPhase == TRAINING_PHASE.phase_1 )
                m_learningRate_P1 = m_appSettings.startLearningRate_P1 * (float)Math.Exp( -(float)m_currentIteration / (float)m_appSettings.numIterations_P1 );
            else if ( m_trainingPhase == TRAINING_PHASE.phase_2 )
                m_learningRate_P2 = m_appSettings.startLearningRate_P2 * (float)Math.Exp( -(float)m_currentIteration / (float)m_appSettings.numIterations_P2 );
            ++m_currentIteration;

            return false;
        }

        private CNode findBMU( SOM_Image_Form.InputVector inputVec ) {
            CNode winner = null;

            float lowestDistance = 999999.0f;

            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {
                    float dist = m_network[ i,j ].calculateDistanceSquared( inputVec );

                    if ( dist < lowestDistance ) {
                        lowestDistance = dist;
                        winner = m_network[ i,j ];
                    }
                }
            }

            System.Diagnostics.Debug.Assert( winner != null );
            return winner;
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
            float numWeightSquareRoot = (float)Math.Sqrt( (double)SOM_Image_Form.NUM_WEIGHTS );

            // Find the average of all the node distances (add up the 8 surrounding nodes / 8).
            for ( int i = 0; i < SOM_Image_Form.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOM_Image_Form.NUM_NODES_ACROSS; ++j ) {
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
                            sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i-1,j-1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }

                        // Top middle.
                        sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i-1,j ].getWeights(), takeSquareRoot );
                        ++neighborCount;

                        // Top right.
                        if ( j < SOM_Image_Form.NUM_NODES_ACROSS - 1 ) {
                            sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i-1,j+1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }
                    }
                    
                    // Left (1).
                    if ( j >= 1 ) {
                        sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i,j-1 ].getWeights(), takeSquareRoot );
                        ++neighborCount;
                    }

                    // Right (1).
                    if ( j < SOM_Image_Form.NUM_NODES_ACROSS - 1 ) {
                        sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i,j+1 ].getWeights(), takeSquareRoot );
                        ++neighborCount;
                    }

                    // Bottom row.
                    if ( i < SOM_Image_Form.NUM_NODES_DOWN - 1 ) {
                        // Bottom left.
                        if ( j >= 1 ) {
                            sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i+1,j-1 ].getWeights(), takeSquareRoot );
                            ++neighborCount;
                        }

                        // Bottom middle.
                        sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i+1,j ].getWeights(), takeSquareRoot );
                        ++neighborCount;

                        // Bottom right.
                        if ( j < SOM_Image_Form.NUM_NODES_ACROSS - 1 ) {
                            sumDistance = SOM_Image_Form.util_getDistance( centerPoint, m_network[ i+1,j+1 ].getWeights(), takeSquareRoot );
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
                    else                  { scaledDistance = (float)SOM_Image_Form.NUM_WEIGHTS * averageDistance; }
                    if ( scaledDistance > 1.0f ) {      scaledDistance = 1.0f;  }
                    else if ( scaledDistance < 0.0f ) { scaledDistance = 0.0f;  }

                    // Create a greyscale (i.e. r = g = b).
                    m_window01[ i,j,0 ] = m_window01[ i,j,1 ] = m_window01[ i,j,2 ] = scaledDistance;

                } // End for each column.
            } // End for each row.

            return totalError;
        }

        /* Using the "most recent BMU" of every image, this will go through and attach an image to
         * a node in the network for later retrieval.
         */
        private void associateImageWithNode() {
            for ( int i = 0; i < m_appSettings.images.Count; ++i ) {
                SOM_Image_Form.ImageData anImage = (SOM_Image_Form.ImageData)m_appSettings.images[ i ];

                // Skip this image if no valid BMU assigned.
                if ( anImage.m_BMU.X < 0 || anImage.m_BMU.Y < 0 )
                    continue;

                m_network[ anImage.m_BMU.X, anImage.m_BMU.Y ].addImage( anImage.m_fileName );
            }
        }
    }
} // End namespace SOM_Image