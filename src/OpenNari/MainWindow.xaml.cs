using System.Globalization;
using System.Runtime.InteropServices;
#if RELEASE_BUILD
using System.Reflection;
#endif
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OpenNari.Core;

namespace OpenNari;

public partial class MainWindow : Window
{
    private NariDevice? headset;
    private bool isBusy;
    private bool isReadingBattery;
    private int batteryGeneration;
    private int missedBatteryReads;
    private readonly BatteryLevelEstimator batteryEstimator = new();
    private readonly DispatcherTimer batteryTimer = new() { Interval = TimeSpan.FromSeconds(15) };

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += Window_SourceInitialized;
        batteryTimer.Tick += BatteryTimer_Tick;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        // Keep the native title bar and Windows 11 window controls while
        // matching the light glass surface below them.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var captionColor = 0x00F0F1EA;
        var textColor = 0x002B2C18;
        DwmSetWindowAttribute(handle, 35, ref captionColor, sizeof(int));
        DwmSetWindowAttribute(handle, 36, ref textColor, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int valueSize);

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
#if RELEASE_BUILD
        var appVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "unknown";
        VersionText.Text = $"Version {appVersion}";
        VersionText.Visibility = Visibility.Visible;
#endif
        await ConnectAsync();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        batteryTimer.Stop();
        batteryGeneration++;
        headset?.Dispose();
    }

    private async void BatteryTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshBatteryAsync();
    }

    private async void Reconnect_Click(object sender, RoutedEventArgs e)
    {
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        batteryTimer.Stop();
        batteryGeneration++;
        isBusy = true;
        UpdateButtons();
        headset?.Dispose();
        headset = null;
        missedBatteryReads = 0;
        ShowBatteryStatus(null);
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
            if (headset is not null)
            {
                batteryTimer.Start();
                await RefreshBatteryAsync();
            }
        }
    }

    private async Task RefreshBatteryAsync()
    {
        if (headset is null || isBusy || isReadingBattery)
        {
            return;
        }

        isReadingBattery = true;
        var device = headset;
        var generation = batteryGeneration;
        try
        {
            var battery = await Task.Run(device.ReadBatteryStatus);
            if (generation == batteryGeneration && ReferenceEquals(device, headset))
            {
                if (battery is { } reading)
                {
                    missedBatteryReads = 0;
                    ShowBatteryStatus(batteryEstimator.Update(reading, DateTimeOffset.UtcNow));
                }
                else if (++missedBatteryReads >= 3)
                {
                    ShowBatteryStatus(null);
                }
            }
        }
        catch (Exception)
        {
            if (generation == batteryGeneration)
            {
                if (++missedBatteryReads >= 3)
                {
                    ShowBatteryStatus(null);
                }
            }
        }
        finally
        {
            isReadingBattery = false;
        }
    }

    private void ShowBatteryStatus(BatteryEstimate? battery)
    {
        if (battery is null)
        {
            BatteryPercentText.Text = "--%";
            ChargeStateText.Text = "Battery unavailable";
            BatteryFill.Width = 0;
            ChargeBolt.Visibility = Visibility.Collapsed;
            return;
        }

        var status = battery.Value;
        BatteryPercentText.Text = $"~{status.Percent}%";
        ChargeStateText.Text = status.ChargeState switch
        {
            BatteryChargeState.Charging => "Charging",
            BatteryChargeState.FullyCharged => "Fully charged",
            _ => "Not charging"
        };
        BatteryFill.Width = 19 * status.Percent / 100.0;
        BatteryFill.Background = new SolidColorBrush(status.Percent switch
        {
            <= 20 => Color.FromRgb(196, 77, 69),
            <= 50 => Color.FromRgb(205, 145, 51),
            _ => Color.FromRgb(19, 123, 100)
        });
        ChargeBolt.Visibility = status.ChargeState == BatteryChargeState.Charging
            ? Visibility.Visible
            : Visibility.Collapsed;
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
