using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Timers;
using System.Diagnostics;

namespace Advanced_clicker
{
    public partial class Form1 : Form
    {
        bool wrongKey = false;
        bool threadExitting = false;
        bool createNewTimer = false;
        static bool changeLine = true;

        int line = -1;
        int waitInterval = 0;

        char lastChar = ' ';
        char systemSeparator = '.';

        Thread t;
        System.Timers.Timer time = new System.Timers.Timer();

        string rootDir = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\Advanced clicker";
        string recentDir = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\Advanced clicker\\recent.txt";


        //Hotkey IMPORT
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        //Global mouse click import
        //This is a replacement for Cursor.Position in WinForms
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public Form1()
        {
            InitializeComponent();
            systemSeparator = Convert.ToChar(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            int id = 0;     // The id of the hotkey. 
            RegisterHotKey(this.Handle, id, (int)KeyModifier.Control, Keys.E.GetHashCode());       // Register Shift + E as global hotkey. 

            if (File.Exists(recentDir))
            {
                foreach (string line in File.ReadLines(recentDir))
                {
                    if (File.Exists(line))
                    {
                        listBox2.Items.Clear();
                        checkBox1.Checked = false;
                        btnStart.Enabled = true;
                        checkBox1.Enabled = true;

                        foreach (string line2 in File.ReadLines(line))
                        {
                            string[] lineArr = line2.Split(' ').ToArray();
                            if (lineArr[0] == "Wait" && !lineArr[1].Contains(systemSeparator.ToString()))
                            {
                                if (systemSeparator == '.')
                                    lineArr[1] = lineArr[1].Replace(',', '.');
                                else
                                    lineArr[1] = lineArr[1].Replace('.', ',');
                            }
                            if (lineArr[0] == "Wait")
                                listBox2.Items.Add($"{lineArr[0]} {lineArr[1]}");
                            else
                                listBox2.Items.Add(line2);
                        }
                    }
                }
            }

            Image startImg = Properties.Resources.play;
            this.btnStart.Image = (Image)(new Bitmap(startImg, new Size(40, 40)));
            Image stopImg = Properties.Resources.stop;
            this.btnStop.Image = (Image)(new Bitmap(stopImg, new Size(40, 40)));
            Image upImg = Properties.Resources.up;
            this.btnUp.Image = (Image)(new Bitmap(upImg, new Size(15, 15)));
            Image downImg = Properties.Resources.down;
            this.btnDown.Image = (Image)(new Bitmap(downImg, new Size(15, 15)));
        }

        private void Execution()
        {
            try
            {
                while (!threadExitting)
                {
                    MethodInvoker mi = delegate ()
                    {
                        if (createNewTimer)
                        {
                            time = new System.Timers.Timer();
                            time.Elapsed += new ElapsedEventHandler(TimeEvent);
                            time.Interval = waitInterval;
                            time.Start();
                            createNewTimer = false;
                        }
                        if (changeLine)
                        {
                            changeLine = false;
                            if (line == listBox2.Items.Count - 1)
                            {
                                if (checkBox1.Checked)
                                {
                                    line = 0;
                                    if (time.Enabled)
                                        time.Stop();
                                }
                                else
                                {
                                    threadExitting = true;
                                    btnStop.PerformClick();
                                }
                            }
                            else
                            {
                                line++;
                                if (time.Enabled)
                                    time.Stop();
                            }

                            if (!threadExitting)
                            {
                                string[] lineArr = listBox2.Items[line].ToString().Split(' ').ToArray();
                                if (lineArr[0] == "Wait")
                                {
                                    createNewTimer = true;
                                    waitInterval = (int)(double.Parse(lineArr[1]) * 1000.0F);
                                }
                                else
                                {
                                    int x = int.Parse(lineArr[1].Substring(1, lineArr[1].Length - 2));
                                    int y = int.Parse(lineArr[2].Substring(0, lineArr[2].Length - 1));

                                    LeftMouseClick(x, y);
                                    changeLine = true;
                                }
                            }
                        }
                    };
                    Invoke(mi);
                }
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                /* Note that the three lines below are not needed if you only want to register one hotkey.
                 * The below lines are useful in case you want to register multiple keys, which you can use a switch with the id as argument, or if you want to know which key/modifier was pressed for some particular reason. */

                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.

                //MessageBox.Show("Hotkey has been pressed!");
                // do something
                if (btnStop.Enabled)
                {
                    btnStop.PerformClick();
                }
            }
        }

        private void loadScript(ListBox lb, bool isScriptEditor)
        {
            bool proceed = false;
            string fileDir = null;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open file";

            ofd.DefaultExt = "txt";
            ofd.Filter = "txt files (*.txt)|*.txt";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    proceed = true;

                    lb.Items.Clear();
                    if (!isScriptEditor)
                        checkBox1.Checked = false;

                    fileDir = ofd.FileName;
                    foreach (string line in File.ReadLines(ofd.FileName))
                    {
                        double a = 0.0F;
                        string[] lineArr = line.Split(' ').ToArray();
                        if (lineArr[0] == "Wait" && !lineArr[1].Contains(systemSeparator.ToString()))
                        {
                            if (systemSeparator == '.')
                                lineArr[1] = lineArr[1].Replace(',', '.');
                            else
                                lineArr[1] = lineArr[1].Replace('.', ',');
                        }
                        if (lineArr[0] != "Wait" && lineArr[0] != "Click")
                            throw new Exception("File corrupt!");
                        if (lineArr[0] == "Wait")
                            a = double.Parse(lineArr[1]);
                        else
                        {
                            string first = lineArr[1].Substring(1, lineArr[1].Length - 2);
                            string second = lineArr[2].Substring(0, lineArr[2].Length - 1);
                            int k = int.Parse(first);
                            int p = int.Parse(second);
                        }
                        if (lineArr[0] == "Wait")
                            lb.Items.Add($"{lineArr[0]} {lineArr[1]}");
                        else
                            lb.Items.Add(line);
                    }
                }
                catch
                {
                    proceed = false;

                    lb.Items.Clear();
                    if (!isScriptEditor)
                        checkBox1.Checked = false;
                    MessageBox.Show("File not compatible!", "Wrong file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (proceed && !isScriptEditor)
            {
                if (!Directory.Exists(rootDir))
                    Directory.CreateDirectory(rootDir);

                StreamWriter sw = new StreamWriter(recentDir, false);
                sw.WriteLine(fileDir);
                sw.Close();

                btnStart.Enabled = true;
                checkBox1.Enabled = true;
            }
        }

        private void btnTip_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To get cursor position click on the coordinate box (0,0), then place your mouse on the desired position and press 'Enter' on your keyboard.", "Tip", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            loadScript(listBox2, false);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char oppositeSeparator;
            if (systemSeparator == '.')
                oppositeSeparator = ',';
            else
                oppositeSeparator = '.';

            if (e.KeyChar == oppositeSeparator)
                e.KeyChar = systemSeparator;
            if (textBox1.Text == "" && e.KeyChar == systemSeparator)
            {
                textBox1.Text = null;
                e.Handled = true;
            }
            if (e.KeyChar == '0' || e.KeyChar == '1' || e.KeyChar == '2' || e.KeyChar == '3'
                || e.KeyChar == '4' || e.KeyChar == '5' || e.KeyChar == '6' || e.KeyChar == '7'
                || e.KeyChar == '8' || e.KeyChar == '9' || e.KeyChar == ',' || e.KeyChar == '.'
                || e.KeyChar == (char)Keys.Back)
            {
                if (lastChar == systemSeparator && e.KeyChar == systemSeparator)
                    e.Handled = true;
                else
                    lastChar = e.KeyChar;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (wrongKey)
            {
                wrongKey = false;
                textBox1.Text = $"0{systemSeparator}000";
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                textBox2.Text = $"({Cursor.Position.X}, {Cursor.Position.Y})";
            }
            else
            {
                wrongKey = true;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (wrongKey)
            {
                wrongKey = false;
                textBox2.Text = "(0, 0)";
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = String.Format("{0:F3}", double.Parse(textBox1.Text) + 1.0F);
            }
            catch
            {
                textBox1.Text = $"1{systemSeparator}000";
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (double.Parse(textBox1.Text) - 1.0F < 0.0F)
                    textBox1.Text = $"0{systemSeparator}000";
                else
                    textBox1.Text = String.Format("{0:F3}", double.Parse(textBox1.Text) - 1.0F);
            }
            catch
            {
                textBox1.Text = $"0{systemSeparator}000";
            }

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndices.Count == 0)
            {
                MessageBox.Show("No selected items to delete!", "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            for (int i = listBox1.Items.Count - 1; i > -1; i--)
            {
                if(listBox1.GetSelected(i))
                    listBox1.Items.RemoveAt(i);
            }
        }

        private void btnAddWait_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
                listBox1.Items.Add($"Wait {textBox1.Text}");
        }

        private void btnAddMouse_Click(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
                listBox1.Items.Add($"Click {textBox2.Text}");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            bool proceed = false;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Enter file name to save";
            sfd.DefaultExt = "txt";
            sfd.Filter = "txt files (*.txt)|*.txt";
            sfd.CheckFileExists = false;
            sfd.CheckPathExists = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                proceed = true;

                StreamWriter sw = new StreamWriter(sfd.FileName, false);
                for (int i = 0; i < listBox1.Items.Count; i++)
                    sw.WriteLine(listBox1.Items[i]);
                sw.Close();
            }

            if (proceed)
            {
                listBox1.Items.Clear();
                textBox1.Text = $"0{systemSeparator}000";
                textBox2.Text = "(0, 0)";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 0);       // Unregister hotkey with id 0 before closing the form. You might want to call this more than once with different id values if you are planning to register more than one hotkey.
            try
            {
                threadExitting = true;
                t.Abort();
                Application.ExitThread();
            }
            catch { }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("To stop execution press 'Ctrl + E'!", "Important", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                WindowState = FormWindowState.Minimized;

                threadExitting = false;
                createNewTimer = false;
                changeLine = true;
                line = -1;
                waitInterval = 0;

                btnStart.Enabled = false;
                CreateProgram.Enabled = false;
                checkBox1.Enabled = false;
                btnOpen.Enabled = false;
                btnStop.Enabled = true;
                t = new Thread(new ThreadStart(Execution));
                t.IsBackground = true;
                t.Start();
            }
        }

        private static void TimeEvent(object source, ElapsedEventArgs e)
        {
            changeLine = true;
        }

        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            threadExitting = true;
            t.Abort();

            WindowState = FormWindowState.Normal;
            btnStop.Enabled = false;
            btnOpen.Enabled = true;
            checkBox1.Enabled = true;
            btnOpen.Enabled = true;
            btnStart.Enabled = true;
            CreateProgram.Enabled = true;
        }

        private void pb_donate_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.paypal.com/donate/?hosted_button_id=P7KRW2GXG6Q9U");
        }

        private void btnLoadScript_Click(object sender, EventArgs e)
        {
            loadScript(listBox1, true);
        }

        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.listBox1.SelectedItem == null) return;
            this.listBox1.DoDragDrop(this.listBox1.SelectedItem, DragDropEffects.Move);
        }

        private void listBox1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            Point point = listBox1.PointToClient(new Point(e.X, e.Y));
            int index = this.listBox1.IndexFromPoint(point);
            if (index < 0) index = this.listBox1.Items.Count - 1;
            object data = e.Data.GetData(DataFormats.Text);
            this.listBox1.Items.Remove(data);
            this.listBox1.Items.Insert(index, data);
        }
    }
}
