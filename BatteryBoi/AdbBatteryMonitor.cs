using System.Diagnostics;
using System.Text.RegularExpressions;
using Timer = System.Windows.Forms.Timer;

namespace BatteryBoi;

public class AdbBatteryMonitor
{
    private const int PollIntervalSeconds = 1;
    
    private Timer _timer;
    private bool _lastConnected = false;
    private int _lastBattery = -1;
    private bool _warned = false;

    public int WarnAtChargePercent { get; set; } = 80;

    public event Action DeviceConnected;
    public event Action DeviceDisconnected;
    public event Action<int> BatteryLevelUpdated;
    public event Action<int> WarnThresholdCrossed;

    public void Start()
    {
        _timer = new Timer();
        _timer.Interval = PollIntervalSeconds * 1000;
        _timer.Tick += (s, e) =>
        {
            PollAdb();
        };

        _timer.Start();
    }

    private void PollAdb()
    {
        string adbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "adb.exe");
        string adbOutput = RunAdbCommand(adbPath, "shell dumpsys battery");

        bool connected = !string.IsNullOrEmpty(adbOutput) && !adbOutput.Contains("not found");
        int batteryLevel = ParseBatteryLevel(adbOutput);

        if (connected != _lastConnected)
        {
            if (connected)
            {
                DeviceConnected?.Invoke();
                _lastBattery = -1; // Trigger refresh of UI
            }
            else
            {
                DeviceDisconnected?.Invoke();
            }

            _lastConnected = connected;
            _warned = false; // Reset warning state on connect/disconnect
        }

        if (!connected)
        {
            return;
        }

        if (batteryLevel != _lastBattery)
        {
            _lastBattery = batteryLevel;
            BatteryLevelUpdated?.Invoke(batteryLevel);

            if (!_warned && batteryLevel >= WarnAtChargePercent)
            {
                _warned = true;
                WarnThresholdCrossed?.Invoke(batteryLevel);
            }
        }
    }

    private string RunAdbCommand(string adbPath, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(adbPath)
            };

            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private int ParseBatteryLevel(string dumpsys)
    {
        var match = Regex.Match(dumpsys, @"level: (\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }
}
