using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using zrobts.Helpers;
using zrobts.Models;

namespace zrobts;

public partial class MainWindow : Window
{
    private DriveInfoModel? _selectedDrive;
    private List<DriveInfoModel> _drives = [];

    public MainWindow()
    {
        InitializeComponent();
        LoadDrives();
        CheckAdminStatus();
    }

    private void LoadDrives()
    {
        try
        {
            _drives = DriveHelper.GetAllDrives();
            DrivesList.ItemsSource = _drives;
            
            if (_drives.Count == 0)
            {
                AppendOutput("Nenhum drive fixo encontrado no sistema.");
            }
            else
            {
                AppendOutput($"Encontrados {_drives.Count} drive(s) no sistema.");
                foreach (var drive in _drives)
                {
                    string type = drive.IsSSD ? "SSD" : "HDD";
                    AppendOutput($"  • {drive.DisplayName} - {type} - {drive.FreeSpaceFormatted} livre");
                }
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"[ERRO] Falha ao carregar drives: {ex.Message}");
        }
    }

    private void CheckAdminStatus()
    {
        if (!DriveHelper.IsRunningAsAdmin())
        {
            AdminWarning.Visibility = Visibility.Visible;
            AppendOutput("[AVISO] O aplicativo não está rodando como Administrador.");
        }
    }

    private void DriveCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string driveLetter)
        {
            _selectedDrive = _drives.FirstOrDefault(d => d.Letter == driveLetter);
            
            if (_selectedDrive != null)
            {
                // Visual feedback - highlight selected card
                AppendOutput($"Drive selecionado: {_selectedDrive.DisplayName}");
            }
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDrive == null)
        {
            MessageBox.Show(
                "Por favor, clique em um dos cards de drive acima para selecionar o disco que deseja limpar.",
                "Selecione um Drive",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Alerta especial para SSD
        if (_selectedDrive.IsSSD)
        {
            var ssdResult = MessageBox.Show(
                $"⚠️ ATENÇÃO: O drive {_selectedDrive.DisplayName} é um SSD!\n\n" +
                "A limpeza com cipher.exe pode não ser 100% eficaz em SSDs devido ao 'wear leveling'.\n\n" +
                "Para SSDs, recomenda-se:\n" +
                "• Usar o Secure Erase do fabricante\n" +
                "• Criptografar o disco antes da limpeza\n" +
                "• Considerar destruição física para dados sensíveis\n\n" +
                "Deseja continuar mesmo assim?",
                "Aviso de Segurança - SSD Detectado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (ssdResult != MessageBoxResult.Yes)
            {
                AppendOutput("Operação cancelada pelo usuário (SSD).");
                return;
            }
        }

        // Confirmação final
        var confirmResult = MessageBox.Show(
            $"🛡️ CONFIRMAÇÃO DE LIMPEZA SEGURA\n\n" +
            $"Drive: {_selectedDrive.DisplayName}\n" +
            $"Tipo: {_selectedDrive.DriveType}\n" +
            $"Espaço livre: {_selectedDrive.FreeSpaceFormatted}\n\n" +
            "Este processo irá sobrescrever todo o espaço livre do disco com dados aleatórios.\n\n" +
            "⏱️ O processo pode demorar bastante dependendo do tamanho do espaço livre.\n\n" +
            "Deseja continuar?",
            "Confirmar Limpeza Segura",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.Yes)
        {
            AppendOutput("Operação cancelada pelo usuário.");
            return;
        }

        await ExecuteWipe(_selectedDrive.Letter);
    }

    private async Task ExecuteWipe(string driveLetter)
    {
        string arg = $"/w:{driveLetter}:";

        StartButton.IsEnabled = false;
        InfoButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        OutputBox.Text = "";
        
        AppendOutput($"═══════════════════════════════════════════════════════");
        AppendOutput($"Iniciando processo de apagamento seguro no drive {driveLetter}:");
        AppendOutput($"═══════════════════════════════════════════════════════");
        AppendOutput("");

        await Task.Run(() =>
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "cipher.exe",
                    Arguments = arg,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process proc = new();
                proc.StartInfo = psi;

                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        AppendOutput(e.Data);
                };

                proc.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        AppendOutput($"[ERRO] {e.Data}");
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                AppendOutput($"[ERRO] Ocorreu um erro durante a execução: {ex.Message}");
            }
        });

        ProgressBar.Visibility = Visibility.Collapsed;
        StartButton.IsEnabled = true;
        InfoButton.IsEnabled = true;
        
        AppendOutput("");
        AppendOutput($"═══════════════════════════════════════════════════════");
        AppendOutput("✅ Processo concluído!");
        AppendOutput($"═══════════════════════════════════════════════════════");
    }

    private void LearnMore_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "🛡️ COMO FUNCIONA A LIMPEZA SEGURA\n\n" +
            "Este processo utiliza a ferramenta 'cipher.exe' do Windows para sobrescrever os espaços livres do disco selecionado.\n\n" +
            "📋 O processo realiza 3 passagens:\n" +
            "  1️⃣ Sobrescreve com zeros (0x00)\n" +
            "  2️⃣ Sobrescreve com uns (0xFF)\n" +
            "  3️⃣ Sobrescreve com dados aleatórios\n\n" +
            "Isso impede que arquivos previamente apagados possam ser recuperados por softwares especializados.\n\n" +
            "⚠️ LIMITAÇÕES:\n" +
            "• Em SSDs, o 'wear leveling' pode manter cópias dos dados em blocos não acessíveis\n" +
            "• O processo só limpa o espaço LIVRE, não afeta arquivos existentes\n" +
            "• Laboratórios forenses avançados podem ter técnicas adicionais",
            "Como Funciona?",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void AppendOutput(string text)
    {
        Dispatcher.Invoke(() =>
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            OutputBox.AppendText($"[{timestamp}] {text}{Environment.NewLine}");
            OutputBox.ScrollToEnd();
        });
    }
}

/// <summary>
/// Converter para calcular largura baseada em porcentagem
/// </summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    public static readonly PercentToWidthConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && 
            values[0] is double percent && 
            values[1] is double width)
        {
            return width * (percent / 100.0);
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
