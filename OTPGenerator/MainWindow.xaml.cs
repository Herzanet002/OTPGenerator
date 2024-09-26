using System.Windows;
using System.Windows.Threading;

namespace OTPGenerator;

public partial class MainWindow : Window
{
    private const string CredentialName = "OTPGeneratorSecretKey";
    private string? _secretKey;
    private readonly DispatcherTimer _timer;
    private readonly OtpManager _otpManager;

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
    }

    private void LoadSecretKey()
    {
        _secretKey = Credential.RetrieveCredential(CredentialName);
        if (!string.IsNullOrEmpty(_secretKey))
        {
            SecretKeyLabel.Text = "********"; // Mask the secret key
            UpdateOtpCode();
            UpdateStatus("Secret key loaded successfully.");
        }
        else
        {
            UpdateStatus("No secret key found. Please set a new one.");
        }
    }

    private void SaveSecretKey(string key)
    {
        if (_otpManager.TryBase32Decode(key, out _)
            && Credential.StoreCredential(key, CredentialName))
        {
            _secretKey = key;
            UpdateStatus("Secret key saved successfully.");
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

    private void UpdateOtpCode()
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            return;
        }

        var otpCode = _otpManager.GenerateOtp(_secretKey);
        OtpCodeLabel.Text = otpCode;
        var timeLeft = 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
        TimeoutBar.Value = timeLeft;
        TimeLeftLabel.Text = $"Time left: {timeLeft} seconds";
    }

    private void OnClearSecretButtonClick(object sender, RoutedEventArgs e)
    {
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
            UpdateStatus("Failed to clear secret key.");
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


    private void UpdateStatus(string message)
        => StatusLabel.Text = message;

    private void OnCopyToClipboardButtonClick(object sender, RoutedEventArgs e)
        => Clipboard.SetText(OtpCodeLabel.Text);
}