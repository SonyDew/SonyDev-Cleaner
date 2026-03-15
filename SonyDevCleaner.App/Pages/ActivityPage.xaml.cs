using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SonyDevCleaner.App.Pages;

public partial class ActivityPage : UserControl
{
    public ActivityPage()
    {
        InitializeComponent();
    }

    public RichTextBox LogTextBox => ActivityLogTextBox;

    public void SetLogText(string text)
    {
        ActivityLogTextBox.Document = CreateDocument(text);
        ActivityLogTextBox.ScrollToEnd();
    }

    private static FlowDocument CreateDocument(string text)
    {
        var document = new FlowDocument
        {
            PagePadding = new System.Windows.Thickness(0)
        };

        foreach (var line in text.Split(Environment.NewLine))
        {
            var paragraph = new Paragraph
            {
                Margin = new System.Windows.Thickness(0)
            };

            if (TrySplitTimestamp(line, out var timestamp, out var message))
            {
                var timestampRun = new Run(timestamp)
                {
                    Foreground = new SolidColorBrush(Color.FromArgb(140, 94, 138, 255))
                };
                var messageRun = new Run($" {message}")
                {
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 210, 225, 240))
                };
                paragraph.Inlines.Add(timestampRun);
                paragraph.Inlines.Add(messageRun);
            }
            else
            {
                paragraph.Inlines.Add(new Run(line)
                {
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 210, 225, 240))
                });
            }

            document.Blocks.Add(paragraph);
        }

        return document;
    }

    private static bool TrySplitTimestamp(string line, out string timestamp, out string message)
    {
        timestamp = string.Empty;
        message = string.Empty;

        if (!line.StartsWith('['))
        {
            return false;
        }

        var closingBracket = line.IndexOf(']');
        if (closingBracket <= 0)
        {
            return false;
        }

        timestamp = line[..(closingBracket + 1)];
        message = line[(closingBracket + 1)..].TrimStart();
        return true;
    }
}
