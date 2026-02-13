using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KabooTextMerger.Models;

public sealed class FileEntry : INotifyPropertyChanged
{
    private int _order;
    private TextEncodingOption _selectedEncoding = TextEncodingOption.Auto;
    private string _detectionSummary = "未判定";
    private string _statusMessage = "未確認";

    public FileEntry(string filePath)
    {
        FilePath = filePath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Order
    {
        get => _order;
        set
        {
            if (_order == value)
            {
                return;
            }

            _order = value;
            OnPropertyChanged();
        }
    }

    public string FilePath { get; }

    public TextEncodingOption SelectedEncoding
    {
        get => _selectedEncoding;
        set
        {
            if (_selectedEncoding == value)
            {
                return;
            }

            _selectedEncoding = value;
            OnPropertyChanged();
        }
    }

    public string DetectionSummary
    {
        get => _detectionSummary;
        set
        {
            if (_detectionSummary == value)
            {
                return;
            }

            _detectionSummary = value;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

