using System.Windows;
using CalculatorApp.Properties;

namespace CalculatorApp
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Создание и показ главного окна
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Сохраняем пользовательские настройки перед закрытием
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}
