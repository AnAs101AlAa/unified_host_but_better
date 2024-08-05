namespace unified_host
{
    public partial class unified_host : Form
    {
        private Button nextButton;
        private Button prevButton;
        private Button clearButton;
        private ToolStrip portbar;
        private TextBox fileContentTextBox;
        private ComboBox linesToDisplayComboBox;
        private string[] fileLines;
        private int currentPage;
        private int linesPerPage;
        private Label currentPageIndicator;
        private Font defaultFont; // Add a field to store the default font

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
            nextButton.Location = new Point(380, 35);
            nextButton.Click += new EventHandler(NextButton_Click);

            prevButton = new Button();
            prevButton.Text = "Previous";
            prevButton.BackColor = Color.White;
            prevButton.Location = new Point(300, 35);
            prevButton.Click += new EventHandler(PrevButton_Click);

            clearButton = new Button();
            clearButton.Text = "clear file";
            clearButton.BackColor = Color.White;
            clearButton.Location = new Point(460, 35);
            clearButton.Click += new EventHandler(ClearButton_Click);

            portbar = new ToolStrip();
            portbar.Size = new Size(70, 20);
            portbar.BackColor = Color.White;

            ToolStripDropDownButton file = new ToolStripDropDownButton("File");
            file.AccessibilityObject.Name = "File";
            file.Size = new Size(40, 20);
            ToolStripMenuItem open = new ToolStripMenuItem("open/Browse");
            file.DropDownItems.Add(open);
            portbar.Items.Add(file);
            open.Click += new EventHandler(BrowseButton_Click);

            ToolStripDropDownButton port = new ToolStripDropDownButton("Console");
            port.AccessibilityObject.Name = "Console";
            port.Size = new Size(100, 20);
            ToolStripMenuItem connect = new ToolStripMenuItem("connect");
            port.DropDownItems.Add(connect);
            portbar.Items.Add(port);
            connect.Click += new EventHandler(connectPortClick);

            fileContentTextBox = new TextBox();
            fileContentTextBox.Multiline = true;
            fileContentTextBox.ReadOnly = true;
            fileContentTextBox.ScrollBars = ScrollBars.Both;
            fileContentTextBox.Location = new Point(40, 80);

            linesToDisplayComboBox = new ComboBox();
            linesToDisplayComboBox.Location = new Point(150, 35);
            linesToDisplayComboBox.Items.AddRange(new object[] { "10", "20", "50", "All" });
            linesToDisplayComboBox.SelectedIndex = 0;
            linesToDisplayComboBox.SelectedIndexChanged += new EventHandler(LinesToDisplayComboBox_SelectedIndexChanged);

            currentPageIndicator = new Label();
            currentPageIndicator.Location = new Point(35, 60);
            currentPageIndicator.Text = "Page: 1";

            this.Controls.Add(nextButton);
            this.Controls.Add(prevButton);
            this.Controls.Add(fileContentTextBox);
            this.Controls.Add(linesToDisplayComboBox);
            this.Controls.Add(currentPageIndicator);
            this.Controls.Add(clearButton);
            this.Controls.Add(portbar);

            defaultFont = fileContentTextBox.Font;
        }

        private void connectPortClick(object? sender, EventArgs e)
        {
            Form f = new connect();
            f.Show();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            fileContentTextBox.Size = new Size(this.ClientSize.Width - 80, this.ClientSize.Height - 170);
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
