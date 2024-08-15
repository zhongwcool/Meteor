using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Meteor.Data;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Meteor.Views;

public partial class MainWindow : Window
{
    private readonly Random _random = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // 设置Timer来跟踪鼠标静止时间
            _mouseStillTimer.Interval = TimeSpan.FromSeconds(30);
            _mouseStillTimer.Tick += MouseStillTimer_Tick!; // 当计时器触发时再恢复背景
            AddGradientBackground();
            AddStars();
        };
        // 处理鼠标移动事件
        MouseMove += MainWindow_MouseMove;

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

        _meteorTimer.Interval = TimeSpan.FromSeconds(_random.Next(5, 20));
        _meteorTimer.Tick += MeteorTimer_Tick!;
        _meteorTimer.Start();

        // 初始化鼠标的最后位置
        _lastMousePosition = Mouse.GetPosition(this);
    }

    // 设置窗口大小覆盖所有屏幕
    private void SetWindowToCoverAllScreens()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    #region 渐变背景+动画

    private Point _lastMousePosition; // 用于存储上一次鼠标的位置

    private const int Threshold = 200; // 鼠标移动的阈值，单位像素

    // 定义Timer和BackgroundRectangle用于之后引用
    private readonly DispatcherTimer _mouseStillTimer = new();

    private void MainWindow_MouseMove(object sender, MouseEventArgs args)
    {
        var currentMousePosition = args.GetPosition(this);

        // 计算鼠标移动的距离
        if (!(Math.Sqrt(Math.Pow(currentMousePosition.X - _lastMousePosition.X, 2) +
                        Math.Pow(currentMousePosition.Y - _lastMousePosition.Y, 2)) > Threshold)) return;

        if (SkyCanvas.Children.Contains(_starAreaBackground))
        {
            // 移除背景
            RemoveBackgroundWithAnimation();
            //_starAreaBackground = null;
            // 说明鼠标需要穿透
            MakeMousePenetration();
            _mouseStillTimer.Start();
        }
        else
        {
            _mouseStillTimer.Stop();
            _mouseStillTimer.Start();
        }

        // 更新最后鼠标位置
        _lastMousePosition = currentMousePosition;
    }

    private void RemoveBackgroundWithAnimation()
    {
        // 创建一个双精度动画，减少透明度至0，时长为2秒
        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1.0, // 开始透明度
            To = 0.0, // 结束透明度
            Duration = new Duration(TimeSpan.FromSeconds(1)), // 动画时长
            FillBehavior = FillBehavior.Stop // 动画完成后，设置行为为停止
        };

        // 动画完成后的事件
        fadeOutAnimation.Completed += (s, e) =>
        {
            if (null == _starAreaBackground) return;
            // 将透明度设置为0
            _starAreaBackground.Opacity = 0.0;
            // 从 SkyCanvas 中移除 starAreaBackground
            SkyCanvas.Children.Remove(_starAreaBackground);
        };

        // 开始动画
        _starAreaBackground?.BeginAnimation(OpacityProperty, fadeOutAnimation);
    }


    private void MouseStillTimer_Tick(object sender, EventArgs e)
    {
        // 鼠标静止超过30秒，重新添加背景
        AddGradientBackground();
        // 停止计时器
        _mouseStillTimer.Stop();
        // 鼠标不动不穿透
        CancelMousePenetration();
    }

    private Rectangle? _starAreaBackground;

    private void AddGradientBackground()
    {
        if (null == _starAreaBackground)
        {
            var canvasWidth = (int)SkyCanvas.ActualWidth; // 获取画布的实际宽度
            var canvasHeight = (int)SkyCanvas.ActualHeight; // 获取画布的实际高度

            // 创建渐变笔刷
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };

            // 半透明黑色到透明的渐变
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0, 0, 0), 0)); // 顶部为半透明黑色
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)); // 底部为完全透明

            // 创建表示背景的矩形并应用渐变笔刷
            _starAreaBackground = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = gradientBrush,
                Opacity = 0.0
            };
        }

        // 将背景矩形添加到Canvas的底层
        SkyCanvas.Children.Insert(0, _starAreaBackground);

        // 创建一个双精度动画，增加透明度至1，时长为2秒
        var fadeInAnimation = new DoubleAnimation
        {
            From = 0.0, // 开始透明度
            To = 1.0, // 结束透明度
            Duration = new Duration(TimeSpan.FromSeconds(1)) // 动画时长
        };

        // 开始动画
        _starAreaBackground.BeginAnimation(OpacityProperty, fadeInAnimation);
    }

    #endregion

    #region 在此添加添加星星的方法 AddStar

    private void AddStars()
    {
        for (var i = 0; i < 100; i++)
        {
            var radius = _random.Next(2, 6);
            var star = new Ellipse
            {
                Width = radius,
                Height = radius,
                Fill = Brushes.White
            };

            // 使用一个概率密度函数来决定星星y的分布
            var densityFactor = Math.Pow(_random.NextDouble(), 2); // 平方使得高度分布向上集中
            var y = densityFactor * 300;

            // 星星的位置
            Canvas.SetLeft(star, _random.Next(0, (int)SkyCanvas.ActualWidth));
            Canvas.SetTop(star, y); // 星星仅出现在画布顶部

            // 闪烁动画
            var blinkAnimation = new DoubleAnimation()
            {
                From = 0.2,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(_random.Next(1, 5))),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            star.BeginAnimation(OpacityProperty, blinkAnimation);

            SkyCanvas.Children.Add(star);
        }
    }

    #endregion

    #region 在此添加添加流星的方法 AddMeteor

    private readonly DispatcherTimer _meteorTimer = new();

    private void MeteorTimer_Tick(object sender, EventArgs e)
    {
        AddMeteor();
        _meteorTimer.Interval = TimeSpan.FromSeconds(_random.Next(5, 20));
    }

    private void AddMeteor()
    {
        // 定义流星的起始位置为右上角的某个随机位置
        double startX = _random.Next(0, (int)SkyCanvas.ActualWidth);
        double startY = _random.Next(0, 100); // 起点的高度不变，仍然是原来的最大100像素

        // 定义流星的结束位置
        // 这里减去Y轴的值，因为WPF的坐标系中，Y轴向下是增加，向上是减少
        // 修改X轴的值来改变流星划过的方向
        double endX = startX - 200;
        double endY = _random.Next(100, 200); // 终点的高度在100到200像素之间

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
    // ReSharper disable once InconsistentNaming
    private const int GWL_EXSTYLE = -20;

    // ReSharper disable once InconsistentNaming
    private const int WS_EX_TRANSPARENT = 0x20;

    private void MakeMousePenetration()
    {
        // 获取当前窗口句柄
        var hwnd = new WindowInteropHelper(this).Handle;
        // 设置窗口样式为透明
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT);
    }

    private void CancelMousePenetration()
    {
        // 获取当前窗口句柄
        var hwnd = new WindowInteropHelper(this).Handle;
        // 获取当前窗口的扩展样式
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        // 移除 WS_EX_TRANSPARENT 扩展样式用来关闭鼠标穿透
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
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
        var icon = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Dark/notify.ico"));
        _trayIcon.Icon = new Icon(icon?.Stream!);
        _trayIcon.Text = "银河与流星划过";
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

    private readonly NotifyIcon _trayIcon = new();

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