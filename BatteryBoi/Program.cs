using System.Diagnostics;
using System.Text.RegularExpressions;
using Timer = System.Windows.Forms.Timer;

namespace BatteryBoi;

class BatteryTrayApp
{
    private const int WarnAtChargePercent = 80;
    private const int BlinkInterval = 500;

    private static NotifyIcon s_trayIcon;
    private static Timer s_pollTimer;
    private static Timer s_blinkTimer;
    private static BatteryWidgetForm s_widgetForm;

    // Icons for various states
    private static Icon s_chargingIcon = new Icon("assets/battery-bolt.ico");
    private static Icon s__warningIconIcon = new Icon("assets/battery-exclamation.ico");
    private static Icon s_disconnectedIcon = new Icon("assets/plug.ico");

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        s_chargingIcon = new Icon("assets/battery-bolt.ico");
        s__warningIconIcon = new Icon("assets/battery-exclamation.ico");

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            s_trayIcon.Visible = false;
            s_widgetForm?.Close();
            Application.Exit();
        });

        s_trayIcon = new NotifyIcon()
        {
            Icon = s_chargingIcon,
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "Battery Boi",
        };

        s_widgetForm = new BatteryWidgetForm();
        s_widgetForm.Show();

        s_pollTimer = new Timer();
        s_pollTimer.Interval = 10_000;
        s_pollTimer.Tick += (s, e) => UpdateBattery();
        s_pollTimer.Start();

        s_blinkTimer = new Timer();
        s_blinkTimer.Interval = BlinkInterval;
        s_blinkTimer.Tick += (s, e) =>
        {
            if (s_trayIcon.Icon == s__warningIconIcon)
            {
                s_trayIcon.Icon = s_chargingIcon;
            }
            else
            {
                s_trayIcon.Icon = s__warningIconIcon;
            }
        };            

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
                if (int.Parse(level) < WarnAtChargePercent)
                {
                    s_blinkTimer.Stop();
                    s_trayIcon.Text = $"Android Battery: {level}%";
                    s_widgetForm.BatteryLabel.Text = $"Battery: {level}%";
                }
                else
                {
                    if (!s_blinkTimer.Enabled)
                    {
                        s_blinkTimer.Start();
                    }
                    s_widgetForm.BatteryLabel.ForeColor = Color.Red;
                    s_trayIcon.Text = $"Sufficiently charged: {level}%";
                    s_widgetForm.BatteryLabel.Text = $"Charged: {level}%";
                }
            }
            else
            {
                if (s_blinkTimer.Enabled)
                {
                    s_blinkTimer.Stop();
                }
                s_trayIcon.Icon = s_disconnectedIcon;
                s_trayIcon.Text = "Battery info not found";
                s_widgetForm.BatteryLabel.Text = $"Battery: --";
            }
        }
        catch (Exception ex)
        {
            s_trayIcon.Icon = s_disconnectedIcon;
            s_trayIcon.Text = "ADB error / phone not connected";
            s_widgetForm.BatteryLabel.Text = $"Disconnected";
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string RunAdb(string arguments)
    {
        var adbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "adb.exe");
        if (!File.Exists(adbPath))
        {
            throw new FileNotFoundException("Bundled ADB executable not found.", adbPath);
        }

        var startInfo = new ProcessStartInfo(adbPath, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo);
        return process.StandardOutput.ReadToEnd();
    }
}
