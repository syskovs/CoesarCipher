using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CoesarCipher.Core;
using Microsoft.Win32;

namespace CoesarCipher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly Brush OkBrush = Brushes.Black;
    private static readonly Brush ErrBrush = Brushes.Firebrick;
    private static readonly Brush HintBrush = Brushes.SteelBlue;

    public MainWindow()
    {
        InitializeComponent();
    }

    private CipherMode CurrentMode =>
        EncryptRadio.IsChecked == true ? CipherMode.Encrypt : CipherMode.Decrypt;

    private bool TryGetKey(out int key)
    {
        if (int.TryParse(KeyTextBox.Text, out key)) return true;
        SetStatus("Ключ должен быть целым числом.", ErrBrush);
        return false;
    }

    private void Process()
    {
        if (!IsLoaded) return;
        if (!TryGetKey(out int key))
        {
            OutputTextBox.Clear();
            return;
        }
        try
        {
            string input = InputTextBox.Text ?? "";
            OutputTextBox.Text = CaesarCipher.Process(input, key, CurrentMode);
            SetStatus($"Готово. Символов: {OutputTextBox.Text.Length}.", OkBrush);
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}", ErrBrush);
        }
    }

    private void SetStatus(string text, Brush brush)
    {
        StatusText.Text = text;
        StatusText.Foreground = brush;
    }

    private void KeyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        foreach (char c in e.Text)
        {
            if (char.IsDigit(c)) continue;
            if (c == '-' && KeyTextBox.CaretIndex == 0 && !KeyTextBox.Text.Contains('-')) continue;
            e.Handled = true;
            return;
        }
    }

    private void KeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (AutoCheckBox?.IsChecked == true) Process();
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (AutoCheckBox?.IsChecked == true) Process();
    }

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (AutoCheckBox?.IsChecked == true) Process();
    }

    private void ProcessButton_Click(object sender, RoutedEventArgs e) => Process();

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        InputTextBox.Clear();
        OutputTextBox.Clear();
        SetStatus("Очищено.", OkBrush);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(OutputTextBox.Text))
        {
            SetStatus("Результат пуст — нечего копировать.", ErrBrush);
            return;
        }
        try
        {
            Clipboard.SetText(OutputTextBox.Text);
            SetStatus("Скопировано в буфер обмена.", HintBrush);
        }
        catch (Exception ex)
        {
            SetStatus($"Не удалось скопировать: {ex.Message}", ErrBrush);
        }
    }

    private void LoadFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
            Title = "Открыть текстовый файл"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            InputTextBox.Text = File.ReadAllText(dlg.FileName, Encoding.UTF8);
            SetStatus($"Загружено: {dlg.FileName}", OkBrush);
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка чтения: {ex.Message}", ErrBrush);
        }
    }

    private void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(OutputTextBox.Text))
        {
            SetStatus("Результат пуст — нечего сохранять.", ErrBrush);
            return;
        }
        var dlg = new SaveFileDialog
        {
            Filter = "Текстовые файлы (*.txt)|*.txt",
            FileName = "result.txt",
            Title = "Сохранить результат"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dlg.FileName, OutputTextBox.Text, new UTF8Encoding(false));
            SetStatus($"Сохранено: {dlg.FileName}", OkBrush);
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка записи: {ex.Message}", ErrBrush);
        }
    }

    private void BatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetKey(out int key)) return;

        var inputDlg = new OpenFolderDialog { Title = "Папка с исходными файлами (*.txt)" };
        if (inputDlg.ShowDialog() != true) return;

        var outputDlg = new OpenFolderDialog { Title = "Папка для результатов" };
        if (outputDlg.ShowDialog() != true) return;

        try
        {
            var result = BatchProcessor.ProcessDirectory(
                inputDlg.FolderName, outputDlg.FolderName, key, CurrentMode);

            string msg = $"Пакетная обработка: обработано {result.Processed}, ошибок {result.Failed}.";
            if (result.Failed > 0)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Ошибки пакетной обработки",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStatus(msg, ErrBrush);
            }
            else
            {
                SetStatus(msg, OkBrush);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка пакетной обработки: {ex.Message}", ErrBrush);
        }
    }
}