namespace NextIteration.SpectreConsole.Settings.Tests.Infrastructure;

/// <summary>Mode enum exercised by the sample settings (round-trips as a string).</summary>
public enum SampleMode
{
    First,
    Second,
}

/// <summary>
/// Representative consumer settings class used across the test suite: a
/// string, an int, and an enum, each backed by a field and calling
/// <c>OnPropertyChanged()</c> from its setter.
/// </summary>
public sealed class SampleSettings : SettingsBase
{
    private string _name = "default-name";
    private int _count = 1;
    private SampleMode _mode = SampleMode.First;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            OnPropertyChanged();
        }
    }

    public SampleMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            OnPropertyChanged();
        }
    }
}

/// <summary>A second settings class, so multi-class and reset-all behaviour can be tested.</summary>
public sealed class SecondarySettings : SettingsBase
{
    private bool _enabled = true;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            OnPropertyChanged();
        }
    }
}
