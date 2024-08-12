using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace unified_host
{
    public partial class sequenceConsole : Form
    {
        public TextBox console;
        public sequenceConsole()
        {
            this.Size = new Size(600, 400);
            InitializeComponent();
            initializeCustomComponents();
        }

        public void initializeCustomComponents()
        {
            console = new TextBox();
            console.Multiline = true;
            console.ScrollBars = ScrollBars.Both;
            console.Dock = DockStyle.Fill;
            console.ReadOnly = true;
            Controls.Add(console);

        }

        public void addLine(string line)
        {
            console.AppendText(line + Environment.NewLine);
        }
    }
}
