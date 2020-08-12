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

namespace MyFirstTestClient
{
    using System.ComponentModel;
    using MyFirstTestClient.ViewModels;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Task.Run(() => this.OnClosing());
        }

        private async Task OnClosing()
        {
            await SubscriberViewModel.OnClosing();
        }
    }
}
