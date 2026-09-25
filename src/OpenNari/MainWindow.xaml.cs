using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using OpenNari.Core;

namespace OpenNari;

public partial class MainWindow : Window
{
    private NariDevice? headset;
    private bool isBusy;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var appVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "0.1.0";
        VersionText.Text = $"Version {appVersion}";
        await ConnectAsync();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        headset?.Dispose();
    }

    private async void Reconnect_Click(object sender, RoutedEventArgs e)
    {
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        isBusy = true;
        UpdateButtons();
        headset?.Dispose();
        headset = null;
        ConnectionText.Text = "Looking for receiver";
        ConnectionDetail.Text = "Checking USB interface 5, collection 3";
        ConnectionIndicator.Fill = new SolidColorBrush(Color.FromRgb(213, 156, 66));

        try
        {
            headset = await Task.Run(NariDevice.Connect);
            if (headset is null)
            {
                ConnectionText.Text = "Receiver not found";
                ConnectionDetail.Text = "Connect the Nari Ultimate receiver";
                ConnectionIndicator.Fill = new SolidColorBrush(Color.FromRgb(189, 69, 59));
                ShowStatus("The headset settings interface is unavailable.", true);
            }
            else
            {
                ConnectionText.Text = "Receiver connected";
                ConnectionDetail.Text = "Razer Nari Ultimate · 1532:051A";
                ConnectionIndicator.Fill = new SolidColorBrush(Color.FromRgb(39, 151, 105));
                HapticsStateText.Text = "No haptic command sent in this session";
                LightingStateText.Text = "No lighting command sent in this session";
                ShowStatus("Connected. Choose a setting to send to the headset.");
            }
        }
        catch (Exception error)
        {
            ConnectionText.Text = "Could not connect";
            ConnectionDetail.Text = error.Message;
            ConnectionIndicator.Fill = new SolidColorBrush(Color.FromRgb(189, 69, 59));
            ShowStatus("Windows could not open the receiver settings interface.", true);
        }
        finally
        {
            isBusy = false;
            UpdateButtons();
        }
    }

    private void StrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (StrengthValueText is not null)
        {
            StrengthValueText.Text = $"{(int)e.NewValue}%";
        }
    }

    private async void ApplyStrength_Click(object sender, RoutedEventArgs e)
    {
        var strength = (int)StrengthSlider.Value;
        await SendAsync(
            device => device.SetHaptics(true, strength),
            $"HyperSense set to {strength}%",
            () => HapticsStateText.Text = strength == 0
                ? "On at 0% in this session"
                : $"On at {strength}% in this session");
    }

    private async void HapticsOff_Click(object sender, RoutedEventArgs e)
    {
        var strength = (int)StrengthSlider.Value;
        await SendAsync(
            device => device.SetHaptics(false, strength),
            "HyperSense off",
            () => HapticsStateText.Text = "Off command sent in this session");
    }

    private async void LightingOn_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync(
            device => device.SetLightingEnabled(true),
            "Lighting on",
            () => LightingStateText.Text = "On command sent in this session");
    }

    private async void LightingOff_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync(
            device => device.SetLightingEnabled(false),
            "Lighting off",
            () => LightingStateText.Text = "Off command sent in this session");
    }

    private async void ApplyColor_Click(object sender, RoutedEventArgs e)
    {
        var hex = HexColorInput.Text.Trim().TrimStart('#');
        if (hex.Length != 6 ||
            !byte.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            ShowStatus("Enter a six digit color such as #00C878.", true);
            HexColorInput.Focus();
            return;
        }

        var colorText = $"#{red:X2}{green:X2}{blue:X2}";
        HexColorInput.Text = colorText;
        await SendAsync(
            device =>
            {
                device.SetLightingEnabled(true);
                // Synapse spaces the on and color reports apart in the capture.
                Thread.Sleep(100);
                return device.SetLightingColor(red, green, blue);
            },
            $"Color {colorText}",
            () =>
            {
                SelectedColorPreview.Background = new SolidColorBrush(Color.FromRgb(red, green, blue));
                LightingStateText.Text = $"Color {colorText} sent in this session";
            });
    }

    private async Task SendAsync(Func<NariDevice, byte[]> command, string setting, Action updateState)
    {
        if (headset is null || isBusy)
        {
            return;
        }

        isBusy = true;
        UpdateButtons();
        ShowStatus($"Sending {setting.ToLowerInvariant()}…");

        try
        {
            var device = headset;
            await Task.Run(() => command(device));
            updateState();
            ShowStatus($"{setting} command sent to the receiver.");
        }
        catch (Exception error)
        {
            ShowStatus($"Could not send {setting.ToLowerInvariant()}: {error.Message}", true);
        }
        finally
        {
            isBusy = false;
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        var ready = headset is not null && !isBusy;
        ReconnectButton.IsEnabled = !isBusy;
        ApplyStrengthButton.IsEnabled = ready;
        HapticsOffButton.IsEnabled = ready;
        LightingOnButton.IsEnabled = ready;
        LightingOffButton.IsEnabled = ready;
        ApplyColorButton.IsEnabled = ready;
        HexColorInput.IsEnabled = ready;
        StrengthSlider.IsEnabled = ready;
    }

    private void ShowStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(172, 51, 45))
            : (Brush)FindResource("Ink");
    }
}
