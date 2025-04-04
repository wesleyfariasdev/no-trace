using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace zrobts;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = DriveLetterBox.SelectedItem as ComboBoxItem;
        string driveLetter = selectedItem?.Content?.ToString().Replace(":/", "");

        if (string.IsNullOrWhiteSpace(driveLetter) || driveLetter == "Selecione o disco")
        {
            MessageBox.Show("Por favor, selecione um disco válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!System.IO.Directory.Exists($"{driveLetter}:\\"))
        {
            MessageBox.Show($"O drive {driveLetter}: não foi encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string arg = $"/w:{driveLetter}:";

        StartButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        DriveLetterBox.IsEnabled = false;
        OutputBox.Text = "";
        AppendOutput($"Iniciando processo de apagamento seguro no drive {driveLetter}:...");

        await Task.Run(() =>
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cipher.exe",
                    Arguments = arg,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process proc = new Process())
                {
                    proc.StartInfo = psi;

                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                            AppendOutput(e.Data);
                    };

                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                            AppendOutput("[ERRO] " + e.Data);
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"[ERRO] Ocorreu um erro durante a execução: {ex.Message}");
            }
        });

        ProgressBar.Visibility = Visibility.Collapsed;
        StartButton.IsEnabled = true;
        DriveLetterBox.IsEnabled = true;
        AppendOutput("Processo concluído.");
    }

    private void LearnMore_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Este processo utiliza a ferramenta 'cipher.exe' para sobrescrever os espaços livres do disco selecionado.\n\n" +
            "Isso impede que arquivos apagados possam ser recuperados por softwares especializados.",
            "Como funciona a limpeza?",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }


    private void AppendOutput(string text)
    {
        Dispatcher.Invoke(() =>
        {
            OutputBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            OutputBox.ScrollToEnd();
        });
    }
}
