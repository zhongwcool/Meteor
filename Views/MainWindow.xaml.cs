using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
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
    private readonly Random random = new();
    private readonly DispatcherTimer _starTimer = new();
    private readonly DispatcherTimer _meteorTimer = new();

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += MainWindow_SourceInitialized;

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

        _meteorTimer.Interval = TimeSpan.FromSeconds(random.Next(5, 20));
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
        AddStars();
    }

    private void MeteorTimer_Tick(object sender, EventArgs e)
    {
        AddMeteor();
        _meteorTimer.Interval = TimeSpan.FromSeconds(random.Next(5, 20));
    }

    #region 在此添加添加星星的方法 AddStar

    private void AddStars()
    {
        var star = new Ellipse()
        {
            Width = random.Next(2, 5),
            Height = random.Next(2, 5),
            Fill = Brushes.White
        };

        // 使用一个概率密度函数来决定星星y的分布
        var densityFactor = Math.Pow(random.NextDouble(), 2); // 平方使得高度分布向上集中
        var y = densityFactor * 200;

        // 星星的位置
        Canvas.SetLeft(star, random.Next(0, (int)SkyCanvas.ActualWidth));
        Canvas.SetTop(star, y); // 星星仅出现在画布顶部

        // 闪烁动画
        var blinkAnimation = new DoubleAnimation()
        {
            From = 0.2,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromSeconds(random.Next(1, 4))),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        star.BeginAnimation(OpacityProperty, blinkAnimation);

        SkyCanvas.Children.Add(star);
    }

    #endregion

    #region 在此添加添加流星的方法 AddMeteor

    private void AddMeteor()
    {
        // 定义流星的起始位置为右上角的某个随机位置
        double startX = random.Next(0, (int)SkyCanvas.ActualWidth);
        double startY = random.Next(0, 100); // 起点的高度不变，仍然是原来的最大100像素

        // 定义流星的结束位置
        // 这里减去Y轴的值，因为WPF的坐标系中，Y轴向下是增加，向上是减少
        // 修改X轴的值来改变流星划过的方向
        double endX = startX - 200;
        double endY = random.Next(100, 200); // 终点的高度在100到200像素之间

        var meteor = new Line
        {
            Stroke = Brushes.White,
            X1 = startX,
            Y1 = startY,
            X2 = endX,
            Y2 = endY,
            StrokeThickness = 1,
            Opacity = 0.8,
            // 渐变消失效果，使用StrokeStartLineCap 和 StrokeEndLineCap 来制作锐利的尾巴效果
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
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

        // 为流星添加动画
        meteor.BeginAnimation(OpacityProperty, meteorAnimation);

        // 向画布添加流星
        SkyCanvas.Children.Add(meteor);
    }

    #endregion

    #region 鼠标穿透

    // 导入 Windows API
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    // 常量定义
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        // 获取当前窗口句柄
        var hwnd = new WindowInteropHelper(this).Handle;
        // 设置窗口样式为透明
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT);
    }

    #endregion

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
        var icon = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Dark/notify.ico"));
        _trayIcon.Icon = new Icon(icon?.Stream!);
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