using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UstViz.App.Views;

/// <summary>简单的消息框对话框。</summary>
public partial class MessageBoxDialog : Window
{
    public MessageBoxDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
