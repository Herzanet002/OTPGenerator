using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OTPGenerator;

public partial class MainWindow : Window
{
    private const string CredentialName = "OTPGeneratorSecretKey";
    private const string SecretMask = "************";
    private string? _secretKey;
    private readonly DispatcherTimer _timer;
    private readonly OtpManager _otpManager;

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    public MainWindow()
    {
        InitializeComponent();
        _otpManager = new OtpManager();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        LoadSecretKey();
        SetSecretButton.IsEnabled = false;
        ClearSecretButton.IsEnabled = false;
    }

    private void LoadSecretKey()
    {
        _secretKey = Credential.RetrieveCredential(CredentialName);
        if (!string.IsNullOrEmpty(_secretKey))
        {
            SecretKeyLabel.Text = SecretMask; // Mask the secret key
            UpdateOtpCode();
            UpdateStatus("Secret key loaded successfully.");
        }
        else
        {
            UpdateStatus("No secret key found. Please set a new one.", false, true);
        }
    }

    private void SaveSecretKey(string key)
    {
        if (_otpManager.TryBase32Decode(key, out _)
            && Credential.StoreCredential(key, CredentialName))
        {
            _secretKey = key;
            UpdateStatus("Secret key saved successfully.");
            SecretKeyLabel.Text = SecretMask;
        }
        else
        {
            UpdateStatus("Failed to save secret key.");
        }
    }

    private void OnSetSecretButtonClick(object sender, RoutedEventArgs e)
    {
        var newKey = SecretKeyLabel.Text.Trim();
        if (string.IsNullOrEmpty(newKey))
        {
            MessageBox.Show("Please enter a secret key.");
            return;
        }

        SaveSecretKey(newKey);
        UpdateOtpCode();
    }

    private void UpdateOtpCode(bool forceRefresh = false)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            return;
        }

        var otpCode = _otpManager.GenerateOtp(_secretKey, forceRefresh);
        OtpCodeLabel.Text = otpCode;
        var timeLeft = 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
        TimeoutBar.Value = timeLeft;
        TimeLeftLabel.Text = $"Time left: {timeLeft} seconds";
    }

    private void OnClearSecretButtonClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to clear the secret key?", "Confirmation", MessageBoxButton.YesNo)
            != MessageBoxResult.OK)
        {
            return;
        }

        if (Credential.DeleteCredential(CredentialName))
        {
            _secretKey = null;
            SecretKeyLabel.Clear();
            OtpCodeLabel.Text = string.Empty;
            UpdateStatus("Secret key cleared successfully.");
            TimeoutBar.Value = 0;
            TimeLeftLabel.Text = string.Empty;
        }
        else
        {
            UpdateStatus("Failed to clear secret key.", false, true);
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_secretKey))
        {
            return;
        }

        UpdateOtpCode();
    }


    private void UpdateStatus(string message, bool calmMessage = true, bool playExclamationSound = false)
    {
        StatusLabel.Text = message;
        StatusLabel.Foreground = calmMessage
            ? Brushes.Green
            : Brushes.Red;

        if (playExclamationSound)
        {
            SystemSounds.Exclamation.Play();
        }
    }

    private void OnEnableAutoPastingInCiscoRadioButtonClick(object sender, RoutedEventArgs e)
        => EnableAutoPastingInCiscoRadioButton.IsChecked = !EnableAutoPastingInCiscoRadioButton.IsChecked;

    private void OnCopyToClipboardButtonClick(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(OtpCodeLabel.Text);
        if (!EnableAutoPastingInCiscoRadioButton.IsChecked.HasValue ||
            !EnableAutoPastingInCiscoRadioButton.IsChecked.Value)
        {
            UpdateStatus("The verification code has been copied to the clipboard.");
            return;
        }

        var hWnd = WindowUtils.FindWindow(@"Cisco AnyConnect \| TAXSEE\d+");
        if (hWnd != IntPtr.Zero)
        {
            var hEdit = FindThirdEditControl(hWnd);
            if (hEdit != IntPtr.Zero)
            {
                WindowUtils.SetForegroundWindow(hWnd);
                WindowUtils.SendMessageNative(hEdit, 0x000C, 0, OtpCodeLabel.Text);
                UpdateStatus("The verification code has been inserted successfully!");
            }
            else
            {
                UpdateStatus("The `Second Password` input field could not be found.", false, true);
            }
        }
        else
        {
            UpdateStatus("The Cisco AnyConnect code entry window was not found.", false, true);
        }
    }

    private void OnUnlockButtonClick(object sender, RoutedEventArgs e)
    {
        SetSecretButton.IsEnabled = !SetSecretButton.IsEnabled;
        ClearSecretButton.IsEnabled = !ClearSecretButton.IsEnabled;
    }

    private static IntPtr FindThirdEditControl(IntPtr hWnd)
    {
        IntPtr hEdit;

        var hStatic = WindowUtils.FindWindowExNative(hWnd, IntPtr.Zero, "Static", "Second Password:");

        if (hStatic != IntPtr.Zero)
        {
            hEdit = WindowUtils.FindWindowExNative(hWnd, hStatic, "Edit", null);
        }
        else
        {
            hEdit = WindowUtils.FindWindowExNative(hWnd, IntPtr.Zero, "Edit", null);
            for (var i = 0; i < 2; i++)
            {
                hEdit = WindowUtils.FindWindowExNative(hWnd, hEdit, "Edit", null);
            }
        }

        return hEdit;
    }
}