using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkLogger
{
    public partial class Form2 : Form
    {
        private int m_interval = 1000;
        public bool m_closeok = false;

        public Form2()
        {
            InitializeComponent();
            m_closeok = false;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            textBox1.Text = m_interval.ToString();
        }

        public int GetInterval() { return m_interval; }
        public void SetInterval(int val) { m_interval = val; }

        private void button1_Click(object sender, EventArgs e)
        {
            m_closeok = true;
            m_interval = int.Parse(textBox1.Text);
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
