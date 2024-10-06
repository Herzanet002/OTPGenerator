using System.Drawing;
using System.IO.Pipes;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace OTPGenerator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string MutexName = "OTPGeneratorMutex";
    private const string PipeName = "OTPGeneratorPipe";
    private Mutex _mutex = null!;
    private NotifyIcon _notifyIcon = null!;
    private CancellationTokenSource _cancellationTokenSource = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out var createdNew);

        if (!createdNew)
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            try
            {
                client.Connect(1000);
                using var writer = new StreamWriter(client);
                writer.WriteLine("SHOW_WINDOW");
                writer.Flush();
            }
            catch (TimeoutException)
            {
            }

            Shutdown();
            return;
        }

        InitializeNotifyIcon();
        _cancellationTokenSource = new CancellationTokenSource();
        StartNamedPipeServer(_cancellationTokenSource.Token);
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon("app_icon.ico"),
            Visible = true
        };

        _notifyIcon.Click += (_, eventArgs) =>
        {
            if (eventArgs is not MouseEventArgs { Button: MouseButtons.Left })
            {
                return;
            }

            OpenWindow();
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (_, _) => OpenWindow());
        contextMenu.Items.Add("Exit", null, (_, _) => Shutdown());
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void StartNamedPipeServer(CancellationToken cancellationToken)
    {
        var serverThread = new Thread(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                server.WaitForConnection();
                using var reader = new StreamReader(server);
                var message = reader.ReadLine();
                if (message == "SHOW_WINDOW")
                {
                    Dispatcher.Invoke(OpenWindow);
                }
            }
        })
        {
            IsBackground = true
        };
        serverThread.Start();
    }

    private void OpenWindow()
    {
        MainWindow ??= new MainWindow();
        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _notifyIcon.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        base.OnExit(e);
    }
}