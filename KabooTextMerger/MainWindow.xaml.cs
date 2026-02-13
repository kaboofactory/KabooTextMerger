using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KabooTextMerger.Models;
using KabooTextMerger.Services;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDrop = System.Windows.DragDrop;
using Forms = System.Windows.Forms;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace KabooTextMerger;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly MergeService _mergeService = new();
    private Point _dragStartPoint;
    private FileEntry? _draggedEntry;
    private string _folderExtensionFilter = string.Empty;
    private string _outputPath = string.Empty;
    private TextEncodingOption _selectedOutputEncoding = TextEncodingOption.Utf8NoBom;
    private LineEndingOption _selectedOutputLineEnding = LineEndingOption.Crlf;
    private string _statusMessage = "ファイルまたはフォルダを追加してください。";
    private bool _isBusy;

    public MainWindow()
    {
        Files = [];
        InputEncodingOptions = EncodingOptionMapper.InputOptions;
        OutputEncodingOptions = EncodingOptionMapper.OutputOptions;
        OutputLineEndingChoices = LineEndingOptionMapper.Choices;

        InitializeComponent();
        DataContext = this;

        OutputPath = Path.Combine(Environment.CurrentDirectory, "merged.txt");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FileEntry> Files { get; }

    public IReadOnlyList<TextEncodingOption> InputEncodingOptions { get; }

    public IReadOnlyList<TextEncodingOption> OutputEncodingOptions { get; }

    public IReadOnlyList<LineEndingChoice> OutputLineEndingChoices { get; }

    public string OutputPath
    {
        get => _outputPath;
        set
        {
            if (_outputPath == value)
            {
                return;
            }

            _outputPath = value;
            OnPropertyChanged();
        }
    }

    public string FolderExtensionFilter
    {
        get => _folderExtensionFilter;
        set
        {
            if (_folderExtensionFilter == value)
            {
                return;
            }

            _folderExtensionFilter = value;
            OnPropertyChanged();
        }
    }

    public TextEncodingOption SelectedOutputEncoding
    {
        get => _selectedOutputEncoding;
        set
        {
            if (_selectedOutputEncoding == value)
            {
                return;
            }

            _selectedOutputEncoding = value;
            OnPropertyChanged();
        }
    }

    public LineEndingOption SelectedOutputLineEnding
    {
        get => _selectedOutputLineEnding;
        set
        {
            if (_selectedOutputLineEnding == value)
            {
                return;
            }

            _selectedOutputLineEnding = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    private void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "マージ対象ファイルを選択",
            Filter = "Text files|*.txt;*.csv;*.log;*.md|All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddPaths(dialog.FileNames, null);
        }
    }

    private void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseDirectoryExtensions(out var directoryExtensions, out var parseError))
        {
            MessageBox.Show(this, parseError, "拡張子指定エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "再帰取り込みするフォルダを選択",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            AddPaths([dialog.SelectedPath], null, directoryExtensions);
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (FilesListView.SelectedItem is not FileEntry selected)
        {
            return;
        }

        selected.PropertyChanged -= FileEntry_PropertyChanged;
        Files.Remove(selected);
        UpdateOrder();
        StatusMessage = "選択ファイルを削除しました。";
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelected(-1);
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelected(1);
    }

    private void MoveSelected(int direction)
    {
        if (FilesListView.SelectedItem is not FileEntry selected)
        {
            return;
        }

        var index = Files.IndexOf(selected);
        if (index < 0)
        {
            return;
        }

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= Files.Count)
        {
            return;
        }

        Files.Move(index, targetIndex);
        FilesListView.SelectedItem = selected;
        UpdateOrder();
    }

    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "出力ファイルを選択",
            Filter = "Text files|*.txt|All files|*.*",
            FileName = Path.GetFileName(OutputPath),
            InitialDirectory = GetOutputInitialDirectory()
        };

        if (dialog.ShowDialog(this) == true)
        {
            OutputPath = dialog.FileName;
        }
    }

    private async void MergeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Files.Count == 0)
        {
            MessageBox.Show(this, "マージ対象ファイルがありません。", "入力不足", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            MessageBox.Show(this, "出力先を指定してください。", "出力先未指定", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        StatusMessage = "マージ処理を実行中です...";

        try
        {
            var requests = Files
                .Select(file => new FileMergeRequest(file.FilePath, file.SelectedEncoding))
                .ToArray();

            var result = await Task.Run(() => _mergeService.MergeAndWrite(
                requests,
                OutputPath,
                SelectedOutputEncoding,
                SelectedOutputLineEnding));
            StatusMessage = $"完了: {result.SuccessCount} 件成功 / {result.FailureCount} 件失敗";

            if (result.FailureCount > 0)
            {
                var errorText = string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.FilePath}: {error.Message}"));
                MessageBox.Show(
                    this,
                    $"一部ファイルで失敗しました。{Environment.NewLine}{errorText}",
                    "部分成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(this, "マージが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "マージに失敗しました。";
            MessageBox.Show(this, ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            RefreshAllDetections();
        }
    }

    private void FilesListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(FilesListView);
        _draggedEntry = FindVisualParent<ListViewItem>(e.OriginalSource as DependencyObject)?.DataContext as FileEntry;
    }

    private void FilesListView_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedEntry is null)
        {
            return;
        }

        var currentPosition = e.GetPosition(FilesListView);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var dataObject = new DataObject(typeof(FileEntry), _draggedEntry);
        DragDrop.DoDragDrop(FilesListView, dataObject, DragDropEffects.Move);
        _draggedEntry = null;
    }

    private void FilesListView_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(typeof(FileEntry)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void FilesListView_Drop(object sender, DragEventArgs e)
    {
        var insertIndex = GetDropInsertIndex(e.GetPosition(FilesListView));

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (!TryParseDirectoryExtensions(out var directoryExtensions, out var parseError))
            {
                MessageBox.Show(this, parseError, "拡張子指定エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var droppedPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (droppedPaths is not null)
            {
                AddPaths(droppedPaths, insertIndex, directoryExtensions);
            }

            return;
        }

        if (!e.Data.GetDataPresent(typeof(FileEntry)))
        {
            return;
        }

        if (e.Data.GetData(typeof(FileEntry)) is not FileEntry item)
        {
            return;
        }

        var oldIndex = Files.IndexOf(item);
        if (oldIndex < 0)
        {
            return;
        }

        var normalizedIndex = Math.Clamp(insertIndex, 0, Files.Count);
        if (normalizedIndex > oldIndex)
        {
            normalizedIndex--;
        }

        if (normalizedIndex == oldIndex)
        {
            return;
        }

        Files.Move(oldIndex, normalizedIndex);
        FilesListView.SelectedItem = item;
        UpdateOrder();
    }

    private void FileEntry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FileEntry.SelectedEncoding) || sender is not FileEntry entry)
        {
            return;
        }

        RefreshDetection(entry);
    }

    private void AddPaths(IEnumerable<string> rawPaths, int? insertAt, ISet<string>? directoryExtensions = null)
    {
        var expandedFiles = FileCollector.ExpandPaths(rawPaths, out var warnings, directoryExtensions);
        if (expandedFiles.Count == 0)
        {
            StatusMessage = "追加対象ファイルが見つかりませんでした。";
            return;
        }

        var existing = new HashSet<string>(
            Files.Select(file => file.FilePath),
            StringComparer.OrdinalIgnoreCase);

        var insertIndex = insertAt.HasValue
            ? Math.Clamp(insertAt.Value, 0, Files.Count)
            : Files.Count;

        var added = 0;
        foreach (var filePath in expandedFiles)
        {
            if (!existing.Add(filePath))
            {
                continue;
            }

            var entry = new FileEntry(filePath);
            entry.PropertyChanged += FileEntry_PropertyChanged;
            RefreshDetection(entry);

            Files.Insert(insertIndex, entry);
            insertIndex++;
            added++;
        }

        UpdateOrder();

        if (warnings.Count > 0)
        {
            var warningText = string.Join(Environment.NewLine, warnings);
            MessageBox.Show(this, warningText, "一部取り込み警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var extensionInfo = FormatDirectoryExtensions(directoryExtensions);
        StatusMessage = string.IsNullOrEmpty(extensionInfo)
            ? $"{added} 件追加しました。"
            : $"{added} 件追加しました。 (拡張子: {extensionInfo})";
    }

    private bool TryParseDirectoryExtensions(out ISet<string>? directoryExtensions, out string? errorMessage)
    {
        directoryExtensions = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(FolderExtensionFilter))
        {
            return true;
        }

        var tokens = FolderExtensionFilter.Split(
            [',', ';', ' '],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalidChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        foreach (var token in tokens)
        {
            if (token is "*" or "*.*")
            {
                directoryExtensions = null;
                return true;
            }

            var normalized = token.StartsWith("*.", StringComparison.Ordinal)
                ? token[1..]
                : token;
            normalized = normalized.StartsWith(".", StringComparison.Ordinal)
                ? normalized
                : $".{normalized}";

            if (normalized.Length <= 1 || normalized.IndexOfAny(invalidChars) >= 0)
            {
                errorMessage = $"拡張子指定が不正です: {token}{Environment.NewLine}例: .txt,.md または txt;csv";
                return false;
            }

            parsed.Add(normalized);
        }

        directoryExtensions = parsed.Count == 0 ? null : parsed;
        return true;
    }

    private static string FormatDirectoryExtensions(ISet<string>? directoryExtensions)
    {
        if (directoryExtensions is null || directoryExtensions.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", directoryExtensions.OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase));
    }

    private void RefreshAllDetections()
    {
        foreach (var file in Files)
        {
            RefreshDetection(file);
        }
    }

    private void RefreshDetection(FileEntry entry)
    {
        try
        {
            var bytes = File.ReadAllBytes(entry.FilePath);
            var resolution = TextEncodingDetector.ResolveForRead(bytes, entry.SelectedEncoding);

            _ = TextEncodingDetector.Decode(bytes, resolution);
            entry.DetectionSummary = $"{resolution.Label} ({resolution.Reason})";
            entry.StatusMessage = "準備完了";
        }
        catch (Exception ex)
        {
            entry.DetectionSummary = "判定失敗";
            entry.StatusMessage = ex.Message;
        }
    }

    private int GetDropInsertIndex(Point point)
    {
        if (Files.Count == 0)
        {
            return 0;
        }

        for (var index = 0; index < Files.Count; index++)
        {
            if (FilesListView.ItemContainerGenerator.ContainerFromIndex(index) is not ListViewItem item)
            {
                continue;
            }

            var topLeft = item.TranslatePoint(new Point(0, 0), FilesListView);
            var half = topLeft.Y + item.ActualHeight / 2.0;
            if (point.Y < half)
            {
                return index;
            }
        }

        return Files.Count;
    }

    private void UpdateOrder()
    {
        for (var index = 0; index < Files.Count; index++)
        {
            Files[index].Order = index + 1;
        }
    }

    private string GetOutputInitialDirectory()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            return Environment.CurrentDirectory;
        }

        var directory = Path.GetDirectoryName(OutputPath);
        return string.IsNullOrWhiteSpace(directory) ? Environment.CurrentDirectory : directory;
    }

    private static T? FindVisualParent<T>(DependencyObject? child)
        where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T typed)
            {
                return typed;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

