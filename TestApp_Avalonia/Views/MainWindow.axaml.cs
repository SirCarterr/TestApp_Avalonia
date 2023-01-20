using Avalonia.Controls;
using TestApp_Avalonia.ViewModels;

namespace TestApp_Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
