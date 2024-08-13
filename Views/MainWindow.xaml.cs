using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Meteor.Data;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;

namespace Meteor.Views;

public partial class MainWindow : Window
{
    private readonly Random _random = new();
    private readonly DispatcherTimer _starTimer = new();
    private readonly DispatcherTimer _meteorTimer = new();

    public MainWindow()
    {
        InitializeComponent();

        // 设置窗口大小覆盖所有屏幕
        SetWindowToCoverAllScreens();

        // 设置托盘图标
        InitializeTrayMenu();
        // 加载主题
        UpdateNotifyIcon();
        // 监听系统主题变化
        SystemEvents.UserPreferenceChanged += (_, args) =>
        {
            // 当事件是由于主题变化引起的
            if (args.Category == UserPreferenceCategory.General)
            {
                // 这里你可以写代码来处理主题变化，例如，重新加载样式或者资源
                UpdateNotifyIcon();
            }
        };

        _starTimer.Interval = TimeSpan.FromSeconds(0.5);
        _starTimer.Tick += StarTimer_Tick!;
        _starTimer.Start();

        _meteorTimer.Interval = TimeSpan.FromSeconds(_random.Next(5, 20));
        _meteorTimer.Tick += MeteorTimer_Tick!;
        _meteorTimer.Start();
    }

    private void SetWindowToCoverAllScreens()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void StarTimer_Tick(object sender, EventArgs e)
    {
        AddStar();
    }

    private void MeteorTimer_Tick(object sender, EventArgs e)
    {
        AddMeteor();
        _meteorTimer.Interval = TimeSpan.FromSeconds(_random.Next(5, 20));
    }

    // 在此添加添加星星的方法 AddStar
    private void AddStar()
    {
        Ellipse star = new Ellipse()
        {
            Width = _random.Next(2, 5),
            Height = _random.Next(2, 5),
            Fill = Brushes.White
        };

        // 星星的位置
        Canvas.SetLeft(star, _random.Next(0, (int)SkyCanvas.ActualWidth));
        Canvas.SetTop(star, _random.Next(0, 100)); // 星星仅出现在画布顶部

        // 闪烁动画
        var blinkAnimation = new DoubleAnimation()
        {
            From = 0.2,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromSeconds(_random.Next(1, 4))),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        star.BeginAnimation(OpacityProperty, blinkAnimation);

        SkyCanvas.Children.Add(star);
    }

    // 在此添加添加流星的方法 AddMeteor
    private void AddMeteor()
    {
        double startX = _random.Next(0, (int)SkyCanvas.ActualWidth - 100);
        double startY = 0;
        double endX = startX + 100; // 计算流星尾部位置
        double endY = 100; // 流星的轨迹是斜线，因此它的Y值将比起点高100

        Line meteor = new Line()
        {
            Stroke = Brushes.White,
            X1 = startX,
            Y1 = startY,
            X2 = endX,
            Y2 = endY,
            StrokeThickness = 2,
            Opacity = 1
        };

        // 流星动画
        DoubleAnimation meteorAnimation = new DoubleAnimation()
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromSeconds(1.5)),
            FillBehavior = FillBehavior.Stop
        };

        // 动画完成后移除流星
        meteorAnimation.Completed += (s, e) => SkyCanvas.Children.Remove(meteor);

        meteor.BeginAnimation(Line.OpacityProperty, meteorAnimation);

        SkyCanvas.Children.Add(meteor);
    }

    #region 托盘功能区

    // 更改托盘图标的函数
    private void UpdateNotifyIcon()
    {
        var isDark = UseDarkSystemTheme();
        if (isDark)
        {
            // 从资源加载图标
            var iconUri = new Uri("pack://application:,,,/Resources/Dark/notify.ico", UriKind.RelativeOrAbsolute);
            var iconStream = Application.GetResourceStream(iconUri)?.Stream;
            _trayIcon.Icon = new Icon(iconStream!);
        }
        else
        {
            // 从资源加载图标
            var iconUri = new Uri("pack://application:,,,/Resources/Light/notify.ico", UriKind.RelativeOrAbsolute);
            var iconStream = Application.GetResourceStream(iconUri)?.Stream;
            _trayIcon.Icon = new Icon(iconStream!);
        }
    }

    private static bool UseDarkSystemTheme()
    {
        // 在注册表中，Windows保存它的个人设置信息
        // 目前Windows将AppsUseLightTheme键值用于表示深色或浅色主题
        // 该键值位于路径HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize下
        const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string registryValueName = "AppsUseLightTheme";

        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
        var registryValueObject = key?.GetValue(registryValueName)!;

        var registryValue = (int?)registryValueObject;

        // AppsUseLightTheme 0表示深色，1表示浅色
        return registryValue == 0;
    }

    // 初始化托盘菜单
    private void InitializeTrayMenu()
    {
        // 创建托盘图标
        _trayIcon = new NotifyIcon();
        var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Dark/notify.ico"))
            ?.Stream;
        _trayIcon.Icon = new Icon(iconStream!);
        _trayIcon.Text = "银河与划过的流星";
        _trayIcon.Visible = true;

        var trayMenu = new ContextMenuStrip();

        trayMenu.Items.Add("关于", null, OnTrayIconAboutClicked!);
        trayMenu.Items.Add("反馈", null, OnTrayIconTouchClicked);
        trayMenu.Items.Add("退出", null, OnTrayIconExitClicked);

        _trayIcon.ContextMenuStrip = trayMenu;
    }

    private readonly AppConfig _config = AppConfig.CreateInstance();

    private void OnTrayIconAboutClicked(object sender, EventArgs e)
    {
        // 获取当前程序集的版本号
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        MessageBox.Show($"版本：{version}");
    }

    private static void OnTrayIconTouchClicked(object? sender, EventArgs e)
    {
        var window = new FeedbackWindow();
        window.ShowDialog();
    }

    private NotifyIcon _trayIcon;

    private void OnTrayIconExitClicked(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _trayIcon.Dispose();
    }

    #endregion
}