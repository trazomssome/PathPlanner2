using DispensePath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DispenserEditor
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            FormEditDispensePath OControl = new FormEditDispensePath("Hello", "DispensingPath.json");
            //OControl.btnGrab.Click += this.BtnGrab_Click;
            wfhPathEdit.Child = OControl;

            //this.IsVisibleChanged += UCDispensingPath_IsVisibleChanged;
        }
        private void BtnGrab_Click(object sender, EventArgs e)
        {
        }
    }
}
