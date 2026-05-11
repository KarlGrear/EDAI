using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace EDAI.UI.Controls;

public partial class HsvColorPicker : UserControl
{
    // ── Dependency property ───────────────────────────────────────────────────

    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register(
            nameof(SelectedColor), typeof(Color), typeof(HsvColorPicker),
            new FrameworkPropertyMetadata(
                Color.FromRgb(0xFF, 0x6D, 0x00),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedColorChanged));

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    // ── Internal HSV state ────────────────────────────────────────────────────

    private double _hue;           // 0–360
    private double _saturation;    // 0–1
    private double _value = 1;     // 0–1  (brightness)
    private bool _suppressing;

    // ── Constructor ───────────────────────────────────────────────────────────

    public HsvColorPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncFromSelectedColor(SelectedColor);
    }

    // ── External color change ─────────────────────────────────────────────────

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HsvColorPicker p && !p._suppressing)
            p.SyncFromSelectedColor((Color)e.NewValue);
    }

    private void SyncFromSelectedColor(Color c)
    {
        _suppressing = true;
        RgbToHsv(c.R, c.G, c.B, out _hue, out _saturation, out _value);
        RefreshAll(raiseChange: false);
        _suppressing = false;
    }

    // ── Refresh everything from internal HSV state ────────────────────────────

    private void RefreshAll(bool raiseChange = true)
    {
        // Update SV canvas hue color stop
        var hueColor = HsvToColor(_hue, 1, 1);
        HueStop.Color = hueColor;

        // Move SV thumb
        if (SvCanvas.ActualWidth > 0 && SvCanvas.ActualHeight > 0)
        {
            Canvas.SetLeft(SvThumb, _saturation * SvCanvas.ActualWidth - SvThumb.Width / 2);
            Canvas.SetTop(SvThumb, (1 - _value) * SvCanvas.ActualHeight - SvThumb.Height / 2);
        }

        // Move hue thumb
        if (HueCanvas.ActualWidth > 0)
            Canvas.SetLeft(HueThumb, _hue / 360.0 * HueCanvas.ActualWidth - HueThumb.Width / 2);

        // Recompute final color
        var color = HsvToColor(_hue, _saturation, _value);

        // Update preview swatch
        ColorPreview.Fill = new SolidColorBrush(color);

        // Sync RGB / hex inputs
        RInput.Text = color.R.ToString();
        GInput.Text = color.G.ToString();
        BInput.Text = color.B.ToString();
        HexInput.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (raiseChange)
        {
            _suppressing = true;
            SelectedColor = color;
            _suppressing = false;
        }
    }

    // ── SV canvas interaction ─────────────────────────────────────────────────

    private void OnSvMouseDown(object sender, MouseButtonEventArgs e)
    {
        SvCanvas.CaptureMouse();
        ApplySvPoint(e.GetPosition(SvCanvas));
    }

    private void OnSvMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && SvCanvas.IsMouseCaptured)
            ApplySvPoint(e.GetPosition(SvCanvas));
    }

    private void OnSvMouseUp(object sender, MouseButtonEventArgs e) =>
        SvCanvas.ReleaseMouseCapture();

    private void OnSvCanvasSizeChanged(object sender, SizeChangedEventArgs e) =>
        RefreshAll(raiseChange: false);

    private void ApplySvPoint(Point p)
    {
        _saturation = Math.Clamp(p.X / SvCanvas.ActualWidth, 0, 1);
        _value      = Math.Clamp(1 - p.Y / SvCanvas.ActualHeight, 0, 1);
        RefreshAll();
    }

    // ── Hue strip interaction ─────────────────────────────────────────────────

    private void OnHueMouseDown(object sender, MouseButtonEventArgs e)
    {
        HueCanvas.CaptureMouse();
        ApplyHuePoint(e.GetPosition(HueCanvas));
    }

    private void OnHueMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && HueCanvas.IsMouseCaptured)
            ApplyHuePoint(e.GetPosition(HueCanvas));
    }

    private void OnHueMouseUp(object sender, MouseButtonEventArgs e) =>
        HueCanvas.ReleaseMouseCapture();

    private void OnHueCanvasSizeChanged(object sender, SizeChangedEventArgs e) =>
        RefreshAll(raiseChange: false);

    private void ApplyHuePoint(Point p)
    {
        _hue = Math.Clamp(p.X / HueCanvas.ActualWidth, 0, 1) * 360;
        RefreshAll();
    }

    // ── RGB inputs ────────────────────────────────────────────────────────────

    private void OnRgbKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ApplyRgbInputs();
    }

    private void OnRgbLostFocus(object sender, RoutedEventArgs e) => ApplyRgbInputs();

    private void ApplyRgbInputs()
    {
        if (byte.TryParse(RInput.Text, out var r) &&
            byte.TryParse(GInput.Text, out var g) &&
            byte.TryParse(BInput.Text, out var b))
        {
            _suppressing = true;
            RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
            RefreshAll();
            _suppressing = false;
        }
    }

    // ── Hex input ─────────────────────────────────────────────────────────────

    private void OnHexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ApplyHexInput();
    }

    private void OnHexLostFocus(object sender, RoutedEventArgs e) => ApplyHexInput();

    private void ApplyHexInput()
    {
        try
        {
            var hex = HexInput.Text.Trim().TrimStart('#');
            if (hex.Length != 6) return;
            var r = Convert.ToByte(hex[0..2], 16);
            var g = Convert.ToByte(hex[2..4], 16);
            var b = Convert.ToByte(hex[4..6], 16);
            RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
            RefreshAll();
        }
        catch { }
    }

    // ── Eyedropper ────────────────────────────────────────────────────────────

    private void OnEyedropperClick(object sender, RoutedEventArgs e)
    {
        // Full-screen nearly-transparent overlay catches the next click
        var overlay = new Window
        {
            Topmost             = true,
            AllowsTransparency  = true,
            Background          = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
            WindowStyle         = WindowStyle.None,
            WindowState         = WindowState.Maximized,
            Cursor              = Cursors.Cross,
            ShowInTaskbar       = false,
        };

        overlay.PreviewMouseLeftButtonDown += (_, ev) =>
        {
            var pos = overlay.PointToScreen(ev.GetPosition(overlay));
            overlay.Close();
            var c = SampleScreenPixel((int)pos.X, (int)pos.Y);
            RgbToHsv(c.R, c.G, c.B, out _hue, out _saturation, out _value);
            RefreshAll();
        };

        overlay.PreviewKeyDown += (_, ev) =>
        {
            if (ev.Key == Key.Escape) overlay.Close();
        };

        overlay.Show();
    }

    // ── GDI screen sampling ───────────────────────────────────────────────────

    [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr hdc, int x, int y);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    private static Color SampleScreenPixel(int x, int y)
    {
        var hdc = GetDC(IntPtr.Zero);
        var colorRef = GetPixel(hdc, x, y);
        ReleaseDC(IntPtr.Zero, hdc);
        return Color.FromRgb(
            (byte)(colorRef & 0xFF),
            (byte)((colorRef >> 8) & 0xFF),
            (byte)((colorRef >> 16) & 0xFF));
    }

    // ── HSV ↔ RGB conversions ─────────────────────────────────────────────────

    private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;

        if (delta == 0) { h = 0; return; }

        if (max == rf)      h = 60 * (((gf - bf) / delta) % 6);
        else if (max == gf) h = 60 * ((bf - rf) / delta + 2);
        else                h = 60 * ((rf - gf) / delta + 4);

        if (h < 0) h += 360;
    }

    private static Color HsvToColor(double h, double s, double v)
    {
        if (s == 0)
        {
            var gray = (byte)Math.Round(v * 255);
            return Color.FromRgb(gray, gray, gray);
        }

        int i = (int)Math.Floor(h / 60) % 6;
        double f = h / 60 - Math.Floor(h / 60);
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        var (r, g, b) = i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };

        return Color.FromRgb(
            (byte)Math.Round(r * 255),
            (byte)Math.Round(g * 255),
            (byte)Math.Round(b * 255));
    }
}
