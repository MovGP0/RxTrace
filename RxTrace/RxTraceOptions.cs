namespace RxTrace;

public sealed class RxTraceOptions
{
    internal static volatile bool _IsEnabled = true;

    public bool IsEnabled
    {
        get => _IsEnabled;
        set => _IsEnabled = value;
    }

    public int Port { get; set; } = 5000;
}