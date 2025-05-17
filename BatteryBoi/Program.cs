using Timer = System.Windows.Forms.Timer;

namespace BatteryBoi;

class BatteryTrayApp
{
    private const int BlinkInterval = 500;

    private static NotifyIcon s_trayIcon;
    private static Timer s_blinkTimer;
    private static BatteryWidgetForm s_widgetForm;

    private static AdbBatteryMonitor s_monitor = new AdbBatteryMonitor
    {
        WarnAtChargePercent = 80
    };

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
            Icon = s_disconnectedIcon,
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "Battery Boi",
        };

        s_widgetForm = new BatteryWidgetForm();
        s_widgetForm.Show();

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

        s_monitor.DeviceConnected += () =>
        {
            s_trayIcon.Icon = s_chargingIcon;
            s_trayIcon.Text = "Phone connected";
            s_widgetForm.Invoke(() =>
            {
                s_trayIcon.Icon = s_chargingIcon;
                s_trayIcon.Text = "Phone connected";
                s_widgetForm.BatteryLabel.Text = "Connected...";
                s_widgetForm.BatteryLabel.ForeColor = Color.White;
            });
            s_widgetForm.BatteryLabel.ForeColor = Color.White;
        };

        s_monitor.DeviceDisconnected += () =>
        {
            s_widgetForm.Invoke(() =>
            {
                s_trayIcon.Icon = s_disconnectedIcon;
                s_trayIcon.Text = "Phone disconnected";
                s_widgetForm.BatteryLabel.Text = "Disconnected";
                s_widgetForm.BatteryLabel.ForeColor = Color.Gray;
            });
            s_blinkTimer.Stop();
        };

        s_monitor.BatteryLevelUpdated += level =>
        {
            s_blinkTimer.Stop();
            s_widgetForm.Invoke(() =>
            {
                s_trayIcon.Icon = s_chargingIcon;
            s_trayIcon.Text = $"Battery: {level}%";
            s_widgetForm.BatteryLabel.Text = $"Battery: {level}%";
            s_widgetForm.BatteryLabel.ForeColor = Color.White;
            });
        };

        s_monitor.WarnThresholdCrossed += level =>
        {
            s_blinkTimer.Start();
            s_widgetForm.Invoke(() =>
            {
                s_widgetForm.BatteryLabel.Text = $"Charged: {level}%";
            s_widgetForm.BatteryLabel.ForeColor = Color.Red;
            s_trayIcon.Text = $"Charged: {level}%";
            });
        };

        s_monitor.Start();

        Application.Run();
    }
}
