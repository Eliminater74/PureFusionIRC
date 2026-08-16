using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PureFusionIRC.Core.Dcc;

public enum DccDirection
{
    Incoming,
    Outgoing
}

public enum DccStatus
{
    Offered,
    Waiting,
    Connecting,
    Transferring,
    Completed,
    Failed,
    Cancelled,
    Declined
}

public sealed class DccTransfer : INotifyPropertyChanged
{
    private DccStatus _status = DccStatus.Offered;
    private long _transferred;
    private long _bytesPerSecond;
    private string _detail = "";
    private string _filePath = "";

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public DccDirection Direction { get; init; }
    public string PeerNick { get; init; } = "";
    public string FileName { get; init; } = "file";
    public long FileSize { get; init; }
    public bool IsReverse { get; set; }
    public string? Token { get; set; }
    public DccOffer? Offer { get; init; }
    public object? Session { get; set; }

    public string FilePath
    {
        get => _filePath;
        set => Set(ref _filePath, value);
    }

    public DccStatus Status
    {
        get => _status;
        set
        {
            if (Set(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(CanAccept));
                OnPropertyChanged(nameof(IsFinished));
            }
        }
    }

    public long Transferred
    {
        get => _transferred;
        set
        {
            if (Set(ref _transferred, value))
            {
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(TransferredLabel));
            }
        }
    }

    public long BytesPerSecond
    {
        get => _bytesPerSecond;
        set
        {
            if (Set(ref _bytesPerSecond, value))
            {
                OnPropertyChanged(nameof(SpeedLabel));
                OnPropertyChanged(nameof(EtaLabel));
            }
        }
    }

    public string Detail
    {
        get => _detail;
        set => Set(ref _detail, value);
    }

    public double Progress => FileSize <= 0 ? (Status == DccStatus.Completed ? 1 : 0) : Math.Clamp((double)Transferred / FileSize, 0, 1);
    public string TransferredLabel => FileSize > 0
        ? DccParser.FormatBytes(Transferred) + " / " + DccParser.FormatBytes(FileSize)
        : DccParser.FormatBytes(Transferred);
    public string SpeedLabel => BytesPerSecond <= 0 ? "" : DccParser.FormatBytes(BytesPerSecond) + "/s";
    public string EtaLabel
    {
        get
        {
            if (BytesPerSecond <= 0 || FileSize <= Transferred)
            {
                return "";
            }

            var seconds = (FileSize - Transferred) / BytesPerSecond;
            return seconds >= 3600 ? $"{seconds / 3600}h {seconds % 3600 / 60}m left"
                : seconds >= 60 ? $"{seconds / 60}m {seconds % 60}s left"
                : $"{seconds}s left";
        }
    }

    public string StatusLabel => Status switch
    {
        DccStatus.Offered => Direction == DccDirection.Incoming ? "Waiting for you" : "Offered",
        DccStatus.Waiting => IsReverse ? "Waiting for them to open a port (works behind most routers)" : "Waiting for them to connect",
        DccStatus.Connecting => "Connecting…",
        DccStatus.Transferring => Direction == DccDirection.Incoming ? "Receiving" : "Sending",
        DccStatus.Completed => "Saved",
        DccStatus.Failed => "Failed",
        DccStatus.Cancelled => "Cancelled",
        DccStatus.Declined => "Declined",
        _ => Status.ToString()
    };

    public string Headline => Direction == DccDirection.Incoming
        ? FileName + "  from  " + PeerNick
        : FileName + "  to  " + PeerNick;

    public bool CanAccept => Status == DccStatus.Offered && Direction == DccDirection.Incoming;
    public bool CanCancel => Status is DccStatus.Offered or DccStatus.Waiting or DccStatus.Connecting or DccStatus.Transferring;
    public bool IsFinished => Status is DccStatus.Completed or DccStatus.Failed or DccStatus.Cancelled or DccStatus.Declined;
    public bool IsRisky => DccParser.IsRiskyFile(FileName);

    public event PropertyChangedEventHandler? PropertyChanged;
    internal CancellationTokenSource Cts { get; } = new();

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    internal void OnPropertyChanged(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
