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

namespace ASSY_CALLAMR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Controller AppController = new Controller();
        public MainWindow()
        {
            InitializeComponent();
            viewEqp.btnConnected.Click += BtnConnected_Click;
        }

        private void BtnConnected_Click(object sender, RoutedEventArgs e)
        {
            APIMessage mess = new APIMessage
            (
                keyNo:"2",
                message: string.Empty,
                callback: result =>
                {
                    MessageBox.Show($"API Response: [{result.ResultCode}] {result.ResultMessage}");
                }
            );
            AppController.SendApiMessage(mess, "1");
        }
    }
}
