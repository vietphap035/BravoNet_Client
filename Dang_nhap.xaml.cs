using BravoNet_Client.Model;
using DACS_1.Database;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BravoNet_Client
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Dang_nhap : Window
    {
        public ObservableCollection<PcModel> MyDataList { get; set; }
        public PcModel SelectPC { get; set; }
        public Dang_nhap()
        {
            InitializeComponent();
            MyDataList = new ObservableCollection<PcModel>(LoadDataList());
            SelectPC = MyDataList.FirstOrDefault();
            ContentRoot.DataContext = this;

        }

        private List<PcModel> LoadDataList()
        {
            List<PcModel> dataList = [];
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var query = "SELECT pc_code, is_active, pc_number FROM pc";
                var cmd = DatabaseConnection.CreateCommand(query, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var pc = new PcModel
                        {
                            pc_id = reader["pc_code"].ToString(),
                            IsOnline = Convert.ToBoolean(reader["is_active"]),
                            pc_num = reader.GetInt32("pc_number")
                        };
                        dataList.Add(pc);
                    }
                }
            }
            return dataList;
        }
        private async Task<bool> ValidatePC(MySqlConnection con,int pcNumber)
        {
            string pcQuery = "SELECT * FROM pc WHERE pc_number = @pcNumber";
            using var pcCmd = DatabaseConnection.CreateCommand(pcQuery, con);
            pcCmd.Parameters.AddWithValue("@pcNumber", pcNumber);
            using var pcReader = pcCmd.ExecuteReader();

            if (!pcReader.HasRows)
            {
                await ShowDialog("PC Not Found", "The selected PC does not exist.");
                return false;
            }
            else if (pcReader.Read() && Convert.ToBoolean(pcReader["is_active"]))
            {
                await ShowDialog("PC Not Available", "The selected PC is currently not available.");
                return false;
            }
            return true;
        }

        public async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // khoi tao bien dang nhap
            string username = UsernameTextBox.Text;
            string password = PasswordTextBox.Password;
            int pcNumber = SelectPC.pc_num;

            // kiem tra dang nhap
            using (var con = DatabaseConnection.GetConnection())
            {
                try
                {
                    con.Open();
                    string query = "SELECT * FROM accounts WHERE username = @username AND pwd = @password";
                    if (!await ValidatePC(con,pcNumber)) return;
                    using (var cmd = DatabaseConnection.CreateCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        Debug.WriteLine($"Executing query: {query} with parameters: {username}, {password}");
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {

                                string userId = reader["UId"].ToString();
                                reader.Close();
                                query = "SELECT existing_time FROM customer_time WHERE UId = @UId";
                                using (var timeCmd = DatabaseConnection.CreateCommand(query, con))
                                {
                                    timeCmd.Parameters.AddWithValue("@UId", userId);
                                    using (var timeReader = timeCmd.ExecuteReader())
                                    {
                                        if (timeReader.Read())
                                        {
                                            int existingTime = timeReader.GetInt32("existing_time");
                                            if (existingTime <= 0)
                                            {
                                                ContentDialog errorDialog = new()
                                                {
                                                    Title = "Insufficient Time",
                                                    Content = "You do not have enough time to log in.",
                                                    CloseButtonText = "OK",
                                                    XamlRoot = this.Content.XamlRoot
                                                };
                                                await errorDialog.ShowAsync();
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            ContentDialog errorDialog = new()
                                            {
                                                Title = "Error",
                                                Content = "Could not retrieve existing time.",
                                                CloseButtonText = "OK",
                                                XamlRoot = this.Content.XamlRoot
                                            };
                                            await errorDialog.ShowAsync();
                                            return;
                                        }
                                    }
                                }
                                ContentDialog successDialog = new()
                                {
                                    Title = "Login Successful",
                                    Content = "Welcome to DACS1!",
                                    CloseButtonText = "OK",
                                    XamlRoot = this.Content.XamlRoot
                                };
                                await successDialog.ShowAsync();


                                
                                string updateQuery = "UPDATE accounts SET is_online = TRUE WHERE UId = @UId";
                                using (var updateCmd = DatabaseConnection.CreateCommand(updateQuery, con))
                                {
                                    updateCmd.Parameters.AddWithValue("@UId", userId);
                                    updateCmd.ExecuteNonQuery();
                                }
                                string updateLoginQuery = "UPDATE accounts SET last_login = @last_login WHERE UId = @UId";
                                using (var updateCmd = DatabaseConnection.CreateCommand(updateLoginQuery, con))
                                {
                                    updateCmd.Parameters.AddWithValue("@last_login", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@UId", userId);
                                    updateCmd.ExecuteNonQuery();
                                }
                                string updatePcQuery = "UPDATE pc SET is_active = true, UId = @UId WHERE pc_number = @pcNumber";
                                using (var updateCmd = DatabaseConnection.CreateCommand(updatePcQuery, con))
                                {
                                    updateCmd.Parameters.AddWithValue("@pcNumber", pcNumber);
                                    updateCmd.Parameters.AddWithValue("@UId", userId);
                                    updateCmd.ExecuteNonQuery();
                                }
                                
                                Home home = new(pcNumber,userId);
                                home.Activate();
                                this.Close();
                            }
                            else
                            {

                                ContentDialog errorDialog = new()
                                {
                                    Title = "Login Failed",
                                    Content = "Invalid username or password.",
                                    CloseButtonText = "OK",
                                    XamlRoot = this.Content.XamlRoot
                                };
                                await errorDialog.ShowAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi kết nối cơ sở dữ liệu
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"An error occurred while connecting to the database: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }

        }
        private async Task ShowDialog(string title, string content)
        {
            ContentDialog dialog = new()
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

    }

}
