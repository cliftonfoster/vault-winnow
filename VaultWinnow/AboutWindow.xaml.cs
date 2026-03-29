using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace VaultWinnow
{
    public partial class AboutWindow : Window
    {
        public string VersionText { get; }

        public AboutWindow()
        {
            InitializeComponent();
            VersionText = GetInformationalVersion();
            DataContext = this;
        }

        private static string GetInformationalVersion()
        {
            var asm = Assembly.GetExecutingAssembly();

            var info = asm
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(info))
            {
                // Drop build metadata like "+1535f7ab1fd03437e6b43..."
                var plusIndex = info.IndexOf('+');
                if (plusIndex >= 0)
                    info = info[..plusIndex];

                return info;
            }

            return asm.GetName().Version?.ToString() ?? "0.0.0.0";
        }


        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignore
            }

            e.Handled = true;
        }
    }
}
