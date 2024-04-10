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
using System.Windows.Shapes;

using System.Xml.Linq;


namespace Server
{
    /// <summary>
    /// Логика взаимодействия для ClearLog.xaml
    /// </summary>
    public partial class ClearLog : Window
    {
        public ClearLog()
        {
            InitializeComponent();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

        }
    }
}
