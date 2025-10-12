using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DispensePath;

namespace PathEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            //InitializeComponent();

            //FormEditDispensePath frm = new FormEditDispensePath("recipe#1", "Setting");//Figuer
            //frm.Dock = DockStyle.Fill;
            //frm.Visible = true;

            //this.Controls.Add(frm);

            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F11)
                {
                    this.WindowState = (this.WindowState == FormWindowState.Maximized)
                        ? FormWindowState.Normal
                        : FormWindowState.Maximized;
                }
            };

            var frm = new FormEditDispensePath("recipe#1", "Setting");
            frm.Dock = DockStyle.Fill;
            frm.Visible = true;
            this.Controls.Add(frm);

        }
    }
}
