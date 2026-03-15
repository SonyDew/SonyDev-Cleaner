using System.Windows.Controls;

namespace SonyDevCleaner.App.Pages;

public partial class LargeFilesPage : UserControl
{
    public LargeFilesPage()
    {
        InitializeComponent();
    }

    public TextBox FolderPath => FolderTextBox;

    public TextBox MinimumSize => MinimumSizeTextBox;

    public Button BrowseActionButton => BrowseButton;

    public Button AnalyzeActionButton => AnalyzeButton;

    public DataGrid Results => ResultsGrid;

    public TextBlock Summary => SummaryText;
}
