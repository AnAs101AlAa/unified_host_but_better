using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace unified_host
{
    public partial class devicesView : Form
    {
        public Dictionary<IPAddress,sequenceConsole> deviceSonsoles;
        public ComboBox devices;
        public Button openConsole;
        public devicesView(Dictionary<IPAddress,sequenceConsole> deviceSonsoles)
        {
            this.deviceSonsoles = deviceSonsoles;
            this.Size = new Size(510, 400);
            this.Icon = new Icon("icon.ico");
            initializeCustomComponents();
            InitializeComponent();
        }

        public void initializeCustomComponents()
        {
            // Add a DataGridView to the form
            devices = new ComboBox();
            devices.Location = new Point(10, 10);
            devices.Size = new Size(300, 20);
            int i = 1;
            foreach (IPAddress currip in deviceSonsoles.Keys)
            {
                devices.Items.Add("Device " + i + "IP: " + currip);
                i++;
            }

            openConsole = new Button();
            openConsole.Location = new Point(400, 10);
            openConsole.Size = new Size(80, 60);
            openConsole.Text = "Open\nConsole";
            openConsole.Click += new EventHandler(openConsole_Click);

            Controls.Add(devices);
            Controls.Add(openConsole);
        }

        private void openConsole_Click(object sender, EventArgs e)
        {
            // Open the console for the selected device
            if(devices.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a device first");
                return;
            }
            int deviceIndex = devices.SelectedIndex;
            deviceSonsoles[deviceSonsoles.Keys.ElementAt(deviceIndex)].Show();
        }
    }
}
