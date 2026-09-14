using CToC.Server;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;


namespace CToC
{

    
    public partial class MainWindow : Window
    {
        TcpServer Server ;
        public static event Action? StopClient;
        public MainWindow()
        {
            
            InitializeComponent();
            

        }


        private void btn_Click(object sender, RoutedEventArgs e)
        {
            Server = new TcpServer();
            
            if (this.btn.IsChecked == true)
            {

                Server.StartTcpServer();

            }
            else if(btn.IsChecked == false)
            {
                StopClient?.Invoke();

            }
            Server.OnClientConnection += OnClientConnect;
        }
        
        private  void OnClientConnect(bool state)
        {
           if(state==false) this.ConnectionState.Text = "F";
           if (state == true) this.ConnectionState.Text = "T";
        }
    }
}
