using DACS_1.Database;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BravoNet_Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Thongtin : Page
    {
        public int pcNum { get; set; }
        public string UId { get; set; }
        private DateTime loginTime { get; set; }
        private DispatcherTimer timer;
        public Thongtin()
        {
            InitializeComponent();
            
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

        }
        private void Timer_Tick(object sender, object e)
        {
            TimeSpan elapsed = DateTime.Now - loginTime;
            ThoiGianTextBlock.Text = $"{elapsed.Hours:D2}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s";
        }

        public void OnLogout()
        {
            timer.Stop();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var homeWindow = e.Parameter as Home;

            if (homeWindow != null)
            {
                UId = homeWindow.UId;
                pcNum = homeWindow.pcNum;
                LoadLoginTime();
                LoadUserName();
                LoadTime();

            }
        }
        private void LoadLoginTime()
        {
            using(var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT last_login FROM accounts WHERE UId = @UId";
                command.Parameters.AddWithValue("@UId", UId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        loginTime = reader.GetDateTime(0);
                    }
                }
            }
        }
        private void LoadUserName()
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT username FROM accounts WHERE UId = @UId";
                command.Parameters.AddWithValue("@UId", UId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Ten.Text = reader.GetString(0);
                    }
                }
            }
        }
        private void LoadTime()
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT existing_time FROM customer_time WHERE UId = @UId";
                command.Parameters.AddWithValue("@UId", UId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int minutes = reader.GetInt32(0);  // hoặc reader.GetUInt32(0) nếu bạn dùng uint
                        TimeSpan time = TimeSpan.FromMinutes(minutes);
                        Gio.Text = time.ToString(@"hh\:mm\:ss");

                    }
                }
            }
        }
    }
}
