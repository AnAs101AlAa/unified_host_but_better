using System.Data;

namespace unified_host
{
    public partial class unified_host : Form
    {
        private Button nextButton;
        private Button prevButton;
        private Button browseButton;
        private Button clearButton;
        private TextBox fileContentTextBox;
        private ComboBox linesToDisplayComboBox;
        private string[] fileLines;
        private int currentPage;
        private int linesPerPage;
        private Label currentPageIndicator;
        private TextBox portInput;
        private Button confirmPort;
        private Label connectionStatus;
        private Button start;
        private socketServer server;
        private Font defaultFont;

        public unified_host()
        {
            InitializeComponent();
            InitializeCustomComponents();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Lavender;
            this.Size = new Size(1000, 700);
            this.Resize += new EventHandler(Form1_Resize);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
        }

        private void InitializeCustomComponents()
        {
            nextButton = new Button();
            nextButton.Text = "Next";
            nextButton.BackColor = Color.White;
            nextButton.Location = new Point(790, 580);
            nextButton.Click += new EventHandler(NextButton_Click);

            browseButton = new Button();
            browseButton.Text = "Browse";
            browseButton.BackColor = Color.White;
            browseButton.Location = new Point(870, 10);
            browseButton.Click += new EventHandler(BrowseButton_Click);

            prevButton = new Button();
            prevButton.Text = "Previous";
            prevButton.BackColor = Color.White;
            prevButton.Location = new Point(710, 580);
            prevButton.Click += new EventHandler(PrevButton_Click);

            clearButton = new Button();
            clearButton.Text = "clear file";
            clearButton.BackColor = Color.White;
            clearButton.Location = new Point(870, 580);
            clearButton.Click += new EventHandler(ClearButton_Click);

            fileContentTextBox = new TextBox();
            fileContentTextBox.Multiline = true;
            fileContentTextBox.ReadOnly = true;
            fileContentTextBox.ScrollBars = ScrollBars.Both;
            fileContentTextBox.Location = new Point(40, 80);

            linesToDisplayComboBox = new ComboBox();
            linesToDisplayComboBox.Location = new Point(110, 580);
            linesToDisplayComboBox.Items.AddRange(new object[] { "10", "20", "50", "All" });
            linesToDisplayComboBox.SelectedIndex = 0;
            linesToDisplayComboBox.SelectedIndexChanged += new EventHandler(LinesToDisplayComboBox_SelectedIndexChanged);

            currentPageIndicator = new Label();
            currentPageIndicator.Location = new Point(35, 580);
            currentPageIndicator.Text = "Page: 1";

            portInput = new TextBox();
            portInput.Location = new Point(10, 10);
            portInput.Size = new Size(100, 20);

            confirmPort = new Button();
            confirmPort.Text = "Connect";
            confirmPort.Location = new Point(120, 10);
            confirmPort.Click += new EventHandler(ConfirmPort_Click);

            start = new Button();
            start.Text = "start";
            start.Location = new Point(200, 10);
            start.Click += new EventHandler(startcomm);

            connectionStatus = new Label();
            connectionStatus.Location = new Point(10, 40);
            connectionStatus.Size = new Size(200, 20);
            connectionStatus.Text = "Disconnected";

            this.Controls.Add(connectionStatus);
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

            defaultFont = fileContentTextBox.Font;
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
                openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*|Hex Files (*.hex*)|*.hex";
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

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Form1_Resize(sender, e);
        }


        private async void ConfirmPort_Click(object sender, EventArgs e)
        {
            server = new socketServer(int.Parse(portInput.Text), this);
            server.start();

            if (server.isConnected())
            {
                UpdateConnectionStatus("client connected successfully");
            }
            else
            {
                UpdateConnectionStatus("client failed to connect");
                server.stop();
            }
        }

        private void startcomm(object sender, EventArgs e)
        {

        }

        public void UpdateConnectionStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateConnectionStatus), status);
            }
            else
            {
                connectionStatus.Text = status;
            }
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
    }
}
