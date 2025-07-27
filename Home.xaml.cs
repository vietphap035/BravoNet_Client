using DACS_1.Database;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace BravoNet_Client
{
    public sealed partial class Home : Window
    {
        // Delegate và GCHandle để giữ delegate không bị GC dọn
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate newWndProcDelegate;
        private GCHandle procHandle;
        private IntPtr oldWndProc = IntPtr.Zero;

        // Win32 API
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll")]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pnid);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(IntPtr hInstance, string lpIconName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        // Constants
        private const int GWLP_WNDPROC = -4;
        private const uint WM_CLOSE = 0x0010;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
        private const uint NIM_ADD = 0x00000000;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_MESSAGE = 0x00000001;
        private const int WM_TRAYICON = 0x800;

        // Tray Icon struct
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
        }

        public int pcNum { get; set; }
        //public int HdNum { get; set; }
        public string UId { get; set; }
        public Home(int pcNum, string UId)
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);
            InitWindowStyle();
            InitWndProcHook();
            
            this.pcNum = pcNum;
            this.UId = UId;
            LoadHoaDon();
            this.Closed += MainWindow_Closed;
        }

        private void InitWindowStyle()
        {
            var appWindow = GetAppWindowForCurrentWindow();
            appWindow.Resize(new SizeInt32(600, 800));
            var overlappedPresenter = appWindow.Presenter as OverlappedPresenter;
            if (overlappedPresenter != null)
            {
                overlappedPresenter.IsResizable = false;
                overlappedPresenter.IsMaximizable = false;
            }
        }

        private void InitWndProcHook()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            newWndProcDelegate = new WndProcDelegate(CustomWndProc);
            procHandle = GCHandle.Alloc(newWndProcDelegate); // Giữ delegate

            IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(newWndProcDelegate);
            oldWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, newProcPtr);
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_CLOSE)
            {
                HideWindow();
                AddTrayIcon();
                return IntPtr.Zero; // Ngăn tắt app
            }

            if (msg == WM_TRAYICON)
            {
                int eventType = lParam.ToInt32();
                if (eventType == 0x0203) // WM_LBUTTONDBLCLK
                {
                    ShowWindowAgain();
                }
            }

            return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void HideWindow()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_HIDE);
        }

        private void ShowWindowAgain()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_SHOW);
            this.Activate();
        }

        private void AddTrayIcon()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var hIcon = LoadImage(IntPtr.Zero, "Assets/youricon.ico", IMAGE_ICON, 0, 0, LR_LOADFROMFILE);

            NOTIFYICONDATA nid = new()
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = hwnd,
                uID = 1,
                uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                uCallbackMessage = WM_TRAYICON,
                hIcon = hIcon,
                szTip = "BravoNet Client"
            };

            Shell_NotifyIcon(NIM_ADD, ref nid);
        }

        public void ThongTin_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Thongtin), this);
        }
        public void DonHang_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Hoadon), this);
        }
        public void DangXuat_Click(object sender, RoutedEventArgs e)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string updatePcQuery = "UPDATE pc SET is_active = false, UId = Null WHERE pc_number = @pcNumber";
                using (var cmd = DatabaseConnection.CreateCommand(updatePcQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@pcNumber", pcNum);
                    cmd.ExecuteNonQuery();
                }
                Dang_nhap dang_Nhap = new Dang_nhap();
                dang_Nhap.Activate();
                this.Close();
            }
            
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            SetCurrentUserOffline(UId);
        }

        // Cập nhật trạng thái người dùng hiện tại là ngoại tuyến
        private void SetCurrentUserOffline(string uid)
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();

            // Cập nhật trạng thái is_online = 0
            var cmd1 = DatabaseConnection.CreateCommand(
                "UPDATE accounts SET is_online = 0 WHERE UId = @UId", conn);
            cmd1.Parameters.AddWithValue("@UId", uid);
            cmd1.ExecuteNonQuery();
            // Tính thời gian đã đăng nhập
            var lastlogout = DateTime.Now;
            DateTime lastLogin = DateTime.MinValue;
            string role = "";

            // Lấy last_login và role
            var cmd2 = DatabaseConnection.CreateCommand("SELECT last_login, roles FROM accounts WHERE UId = @UId", conn);
            cmd2.Parameters.AddWithValue("@UId", uid);
            using (var reader = cmd2.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (reader["last_login"] != DBNull.Value)
                        lastLogin = Convert.ToDateTime(reader["last_login"]);

                    if (reader["roles"] != DBNull.Value)
                        role = reader["roles"].ToString();
                }
            }

            // Tính thời gian đã sử dụng
            TimeSpan sessionDuration = lastlogout - lastLogin;

            if (role == "customer")
            {
                int existingTime = 0;

                // Lấy thời gian còn lại
                var cmd = DatabaseConnection.CreateCommand("SELECT existing_time FROM customer_time WHERE UId = @UId", conn);
                cmd.Parameters.AddWithValue("@UId", uid);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && reader["existing_time"] != DBNull.Value)
                    {
                        existingTime = Convert.ToInt32(reader["existing_time"]);
                    }
                }

                // Trừ thời gian đã sử dụng
                int updatedTime = existingTime - (int)sessionDuration.TotalMinutes;
                Debug.WriteLine($"Thời gian đã sử dụng: {sessionDuration.TotalMinutes} phút, thời gian còn lại: {updatedTime} phút");
                if (updatedTime < 0) updatedTime = 0;

                // Cập nhật lại thời gian còn lại
                var comd = DatabaseConnection.CreateCommand(
                    "UPDATE customer_time SET existing_time = @time WHERE UId = @UId", conn);
                comd.Parameters.AddWithValue("@time", updatedTime);
                comd.Parameters.AddWithValue("@UId", uid);
                comd.ExecuteNonQuery();
            }
            else
            {
                var cmd = DatabaseConnection.CreateCommand(
                    "UPDATE staffs SET work_time = work_time + @minutes WHERE UId = @UId", conn);
                cmd.Parameters.AddWithValue("@minutes", (int)sessionDuration.TotalMinutes);
                cmd.Parameters.AddWithValue("@UId", uid);
                cmd.ExecuteNonQuery();
            }

            // Cập nhật last_login = NULL
            var cmd3 = DatabaseConnection.CreateCommand(
                "UPDATE accounts SET last_login = NULL WHERE UId = @UId", conn);
            cmd3.Parameters.AddWithValue("@UId", uid);
            cmd3.ExecuteNonQuery();
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        public void LoadHoaDon()
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM orders WHERE UId = @UId";
                Debug.WriteLine(this.UId);
                            
                using (var cmd = DatabaseConnection.CreateCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UId", this.UId);
                    Debug.WriteLine(cmd);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            
                            Debug.WriteLine(reader.GetInt32(0).ToString());
                            hdNum.Text = reader.GetInt32(0).ToString();
                        }
                    }
                }
            }
        }
    }
}
