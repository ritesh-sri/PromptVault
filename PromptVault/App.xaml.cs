using System.Windows;

namespace PromptVault
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure single instance of the application
            bool createdNew;
            var mutex = new System.Threading.Mutex(true, "PromptVault_SingleInstance", out createdNew);

            if (!createdNew)
            {
                // Application is already running
                MessageBox.Show("PromptVault is already running!",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
                return;
            }

            // Keep the mutex alive for the lifetime of the application
            GC.KeepAlive(mutex);
        }
    }
}