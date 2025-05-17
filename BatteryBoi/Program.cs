using System.Diagnostics;
using System.Text.RegularExpressions;
using Timer = System.Windows.Forms.Timer;

namespace BatteryBoi;

class BatteryTrayApp
{
    static NotifyIcon trayIcon;
    static Timer pollTimer;
    static BatteryWidgetForm widgetForm;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        trayIcon = new NotifyIcon()
        {
            Icon = new Icon("battery-bolt.ico"),
            Visible = true,
            Text = "Android Battery: Unknown"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            trayIcon.Visible = false;
            widgetForm?.Close();
            Application.Exit();
        });

        trayIcon.ContextMenuStrip = contextMenu;

        widgetForm = new BatteryWidgetForm();
        widgetForm.Show();

        pollTimer = new Timer();
        pollTimer.Interval = 10_000;
        pollTimer.Tick += (s, e) => UpdateBattery();
        pollTimer.Start();

        UpdateBattery();
        Application.Run();
    }

    static void UpdateBattery()
    {
        try
        {
            var output = RunAdb("shell dumpsys battery");
            var match = Regex.Match(output, @"level: (\d+)");
            if (match.Success)
            {
                var level = match.Groups[1].Value;
                trayIcon.Text = $"Android Battery: {level}%";
                widgetForm.BatteryLabel.Text = $"Battery: {level}%";
            }
            else
            {
                trayIcon.Text = "Battery info not found";
                widgetForm.BatteryLabel.Text = $"Battery: --";
            }
        }
        catch
        {
            trayIcon.Text = "ADB error / phone not connected";
            widgetForm.BatteryLabel.Text = $"Disconnected";
        }
    }

    static string RunAdb(string arguments)
    {
        var startInfo = new ProcessStartInfo("adb", arguments)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        return process.StandardOutput.ReadToEnd();
    }
}
