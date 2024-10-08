using System.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net;

namespace unified_host
{
    public partial class unified_host : Form
    {
        //file browse and preview
        private Button nextButton;
        private Button prevButton;
        private Button browseButton;
        private Button clearButton;
        private TextBox fileContentTextBox;
        private ComboBox linesToDisplayComboBox;
        private Font defaultFont;

        //data storage and manipulation
        private string[] fileLines;
        public List<byte[]> filePackets;
        private int currentPage;
        private int linesPerPage;
        private Label currentPageIndicator;

        //port and ip reading and connection
        private Label portLabel;
        private TextBox portInput;
        private Button confirmPort;
        private Button start;
        private socketServer server;

        //console for following command line
        sequenceConsole console;
        public unified_host()
        {
            InitializeComponent();
            InitializeCustomComponents();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.BackColor = Color.Lavender;
            this.Size = new Size(1000, 700);
            this.Resize += new EventHandler(Form1_Resize);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
            this.Icon = new Icon("icon.ico");
        }

        private void InitializeCustomComponents()
        {
            //--------------------the following is for file browsing and previewing-------------------//

            //next page button
            nextButton = new Button();
            nextButton.Text = "Next";
            nextButton.BackColor = Color.White;
            nextButton.Location = new Point(790, 580);
            nextButton.Click += new EventHandler(NextButton_Click);

            //previous page button
            prevButton = new Button();
            prevButton.Text = "Previous";
            prevButton.BackColor = Color.White;
            prevButton.Location = new Point(710, 580);
            prevButton.Click += new EventHandler(PrevButton_Click);

            //browse file to open button
            browseButton = new Button();
            browseButton.Text = "Browse";
            browseButton.BackColor = Color.White;
            browseButton.Location = new Point(870, 610);
            browseButton.Click += new EventHandler(BrowseButton_Click);

            //clear file previewed button
            clearButton = new Button();
            clearButton.Text = "clear file";
            clearButton.BackColor = Color.White;
            clearButton.Location = new Point(870, 580);
            clearButton.Click += new EventHandler(ClearButton_Click);

            //display file content field
            fileContentTextBox = new TextBox();
            fileContentTextBox.Multiline = true;
            fileContentTextBox.ReadOnly = true;
            fileContentTextBox.ScrollBars = ScrollBars.Both;
            fileContentTextBox.Location = new Point(40, 80);
            
            //lines to display combo box
            linesToDisplayComboBox = new ComboBox();
            linesToDisplayComboBox.Location = new Point(110, 580);
            linesToDisplayComboBox.Items.AddRange(new object[] { "10", "20", "50", "All" });
            linesToDisplayComboBox.SelectedIndex = 0;
            linesToDisplayComboBox.SelectedIndexChanged += new EventHandler(LinesToDisplayComboBox_SelectedIndexChanged);

            //current page number indicator
            currentPageIndicator = new Label();
            currentPageIndicator.Location = new Point(35, 580);
            currentPageIndicator.Text = "Page: 1";

            //--------------------the following is for port operations and starting boot------------------------------//

            portLabel = new Label();
            portLabel.Location = new Point(10, 25);
            portLabel.Text = "Port:";

            //port input field to coonect to
            portInput = new TextBox();
            portInput.Location = new Point(50, 20);
            portInput.Size = new Size(100, 20);

            //connect on selected port
            confirmPort = new Button();
            confirmPort.Text = "Connect";
            confirmPort.Size = new Size(80, 60);
            confirmPort.Location = new Point(750, 10);
            confirmPort.Click += new EventHandler(ConfirmPort_Click);

            //start booting
            start = new Button();
            start.Text = "program\ndevices";
            start.Size = new Size(80, 60);
            start.Enabled = false;
            start.Location = new Point(850, 10);
            start.Click += new EventHandler(startcomm);

            //-------------------------------adding all components to the form----------------------------------//

            this.Controls.Add(portInput);
            this.Controls.Add(confirmPort);
            this.Controls.Add(start);
            this.Controls.Add(nextButton);
            this.Controls.Add(prevButton);
            this.Controls.Add(fileContentTextBox);
            this.Controls.Add(linesToDisplayComboBox);
            this.Controls.Add(currentPageIndicator);
            this.Controls.Add(clearButton);
            this.Controls.Add(browseButton);
            this.Controls.Add(portLabel);

            defaultFont = fileContentTextBox.Font;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Add)
            {
                fileContentTextBox.Font = new Font(fileContentTextBox.Font.FontFamily, fileContentTextBox.Font.Size + 2);
            }
            else if (e.Control && e.KeyCode == Keys.Subtract)
            {
                fileContentTextBox.Font = new Font(fileContentTextBox.Font.FontFamily, fileContentTextBox.Font.Size - 2);
            }
            else if (e.Control && e.KeyCode == Keys.D0)
            {
                fileContentTextBox.Font = defaultFont;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Form1_Resize(sender, e);
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            fileContentTextBox.Size = new Size(this.ClientSize.Width - 80, this.ClientSize.Height - 170);
            currentPageIndicator.Location = new Point(fileContentTextBox.Location.X, fileContentTextBox.Location.Y + 500);
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            fileLines = null;
            UpdateDisplayedText();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Hex Files (*.hex*)|*.hex|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        fileLines = File.ReadAllLines(openFileDialog.FileName);
                        currentPage = 0;
                        currentPageIndicator.Text = $"Page: {currentPage + 1}";
                        UpdateDisplayedText();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }
            }
        }

        private void LinesToDisplayComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileLines == null)
                return;
            currentPage = 0;
            currentPageIndicator.Text = "Page: 1";
            UpdateDisplayedText();
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (fileLines == null)
                return;
            if (linesPerPage != fileLines.Length && (currentPage + 1) * linesPerPage < fileLines.Length)
            {
                currentPage++;
                currentPageIndicator.Text = $"Page: {currentPage + 1}";
                UpdateDisplayedText();
            }
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            if (fileLines == null)
                return;
            if (linesPerPage != fileLines.Length && currentPage > 0)
            {
                currentPage--;
                currentPageIndicator.Text = $"Page: {currentPage - 1}";
                UpdateDisplayedText();
            }
        }

        private void UpdateDisplayedText()
        {
            if (fileLines == null)
            {
                fileContentTextBox.Text = "";
                currentPage = 0;
                currentPageIndicator.Text = "Page: 1";
                return;
            }

            string selectedValue = linesToDisplayComboBox.SelectedItem.ToString();
            if (selectedValue == "All")
            {
                linesPerPage = fileLines.Length;
                currentPageIndicator.Hide();
            }
            else
            {
                linesPerPage = int.Parse(selectedValue);
                currentPageIndicator.Show();
            }

            int startLine = currentPage * linesPerPage;
            int endLine = Math.Min(startLine + linesPerPage, fileLines.Length);
            string fileContent = string.Join(Environment.NewLine, fileLines.Skip(startLine).Take(endLine - startLine));
            fileContentTextBox.Text = fileContent;
        }
        private async void ConfirmPort_Click(object sender, EventArgs e)
        {
            try
            {
                if (console == null)
                {
                    console = new sequenceConsole();
                    console.Show();
                }

                //open sequence follow up ocnsole where commands successes/fails are displayed

                if (server != null)
                {
                    server.stop(this, console);
                    server = null;
                }

                console.Close();
                console = new sequenceConsole();
                console.Show();

                int Sport = 65500;
                string hostIp = "192.168.1.2";
                if (portInput.Text != "")
                    Sport = int.Parse(portInput.Text);

                server = new socketServer(Sport);

                //test if devide is connected and respondes to commands
                await server.verifyDeviceConnection(console);
                if (server.pairedDevices == 0)
                    return;
                start.Enabled = true;
            }
            catch (Exception x)
            {
                console.addLine($"can't connect client: {x.Message}");
            }
        }

        private async void startcomm(object sender, EventArgs e)
        {
            if(fileLines == null)
            {
                MessageBox.Show("please select a file to program and try again");
                return;
            }

            if(server == null)
            {
                MessageBox.Show("please connect to a client and try again");
                return;
            }

            console.addLine("parsing hex file...");
            try
            {
                hexParser.parseLinesIntoPackets(fileLines, server);
                console.addLine("hex file parsed");
            }
            catch (Exception x)
            {
                console.addLine($"error parsing hex file: {x.Message}");
                return;
            }

            console.addLine($"starting programming sequence on port:{portInput.Text}...");
            await Task.Delay(2000);
            Dictionary<IPAddress,sequenceConsole> devicesConsoles = await server.programSequence(console);
            await Task.Delay(2000);

            //open a new form to select and display the command stream of each device
            console.addLine("to view each program sequence console, click on the device IP address in the list view");
            devicesView devicesView = new devicesView(devicesConsoles);
            devicesView.Show();

            start.Enabled = false;
            server.stop(this, console);
            server = null;
        }
    }
}
