using System.Runtime.InteropServices;

public class BatteryWidgetForm : Form
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Label BatteryLabel { get; private set; }

    // DLL imports for dragging
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    public BatteryWidgetForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        BackColor = Color.Black;
        ForeColor = Color.LimeGreen;
        Opacity = 0.85;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(100, 100);
        Size = new Size(120, 40);

        BatteryLabel = new Label
        {
            Text = "Battery: --%",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.LimeGreen,
            BackColor = Color.Black
        };

        Controls.Add(BatteryLabel);

        // Make the whole form draggable
        this.MouseDown += DragForm;
        BatteryLabel.MouseDown += DragForm;
    }

    private void DragForm(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }
}
