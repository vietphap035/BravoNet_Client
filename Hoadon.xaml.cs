using BravoNet_Client.Model;
using DACS_1.Database;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Core;

namespace BravoNet_Client
{
    public sealed partial class Hoadon : Page
    {
        public string UId { get; set; }
        public ObservableCollection<HoaDonModel> MyDataList { get; set; } = new();
        private Home homeWindow;
        public Hoadon()
        {
            InitializeComponent();
            DataGridHoaDon.AutoGeneratingColumn += DataGridHoaDon_AutoGeneratingColumn;
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var homeWindow = e.Parameter as Home;
            
            if (homeWindow != null)
            {
                this.homeWindow = homeWindow;
                UId = homeWindow.UId;
                LoadOrderList();
            }
        }

        private void LoadOrderList()
        {
            MyDataList.Clear();
            int STT = 0;
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var query = @"
                    SELECT o.order_id, o.order_date, o.order_status, o.UId, a.username,
                           (SELECT SUM(price_at_order * quantity) FROM orders_items oi WHERE oi.order_id = o.order_id) AS total_price
                    FROM orders o
                    JOIN accounts a ON o.UId = a.UId
                    WHERE o.UId = @UId";

                var cmd = DatabaseConnection.CreateCommand(query, conn);
                cmd.Parameters.AddWithValue("@UId", UId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        STT++;
                        var order = new HoaDonModel
                        {
                            OrderId = reader["order_id"].ToString(),
                            UId = reader["UId"].ToString(),
                            OrderDate = Convert.ToDateTime(reader["order_date"]),
                            Status = Convert.ToBoolean(reader["order_status"]),
                            UserName = reader["username"].ToString(),
                            STT = STT,
                            Tong_tien = reader["total_price"] != DBNull.Value ? Convert.ToDecimal(reader["total_price"]) : 0
                        };
                        MyDataList.Add(order);
                    }
                }
            }

            Debug.WriteLine("MyDataList.Count: " + MyDataList.Count);
        }

        public async void Detail_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HoaDonModel order)
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();
                    var query = "SELECT username FROM accounts WHERE UId = @UId";
                    var cmd = DatabaseConnection.CreateCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UId", order.UId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string username = reader["username"].ToString();
                            string date = order.OrderDate.ToString("dd/MM/yyyy");
                            var statusComboBox = new ComboBox
                            {
                                Width = 200,
                                ItemsSource = new List<string> { "Chưa thanh toán", "Đã thanh toán" },
                                SelectedIndex = order.Status ? 1 : 0
                            };

                            var layout = new StackPanel
                            {
                                Spacing = 10,
                                Children =
                                {
                                    CreateRow("Tên khách hàng:", new TextBlock { Text = username, Width = 200 }),
                                    CreateRow("Thời gian đặt hàng:", new TextBlock { Text = date, Width = 200 }),
                                    CreateRow("Trạng thái:", statusComboBox),
                                    new TextBlock { Text = "Sản phẩm", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) }
                                }
                            };

                            var grid = new Grid
                            {
                                RowSpacing = 5,
                                ColumnSpacing = 10
                            };

                            // Columns
                            grid.ColumnDefinitions.Add(new ColumnDefinition());
                            grid.ColumnDefinitions.Add(new ColumnDefinition());
                            grid.ColumnDefinitions.Add(new ColumnDefinition());
                            grid.ColumnDefinitions.Add(new ColumnDefinition());

                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                            grid.Children.Add(CreateCell("STT", 0, 0, true));
                            grid.Children.Add(CreateCell("Tên món", 0, 1, true));
                            grid.Children.Add(CreateCell("Số lượng", 0, 2, true));
                            grid.Children.Add(CreateCell("Tổng tiền", 0, 3, true));

                            int row = 1;
                            var items = GetItemOrder(order.OrderId);

                            foreach (var item in items)
                            {
                                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                grid.Children.Add(CreateCell(row.ToString(), row, 0));
                                grid.Children.Add(CreateCell(item.name, row, 1));
                                grid.Children.Add(CreateCell(item.quantity.ToString(), row, 2));
                                grid.Children.Add(CreateCell(item.price_at_order.ToString("N0") + " VNĐ", row, 3));
                                row++;
                            }

                            layout.Children.Add(grid);
                            reader.Close();

                            ContentDialog dialog = new()
                            {
                                Title = "Thông tin đơn hàng",
                                Content = layout,
                                PrimaryButtonText = "Xác nhận",
                                XamlRoot = this.XamlRoot
                            };

                            var result = await dialog.ShowAsync();
                            if (result == ContentDialogResult.Primary)
                            {
                                bool newStatus = statusComboBox.SelectedIndex == 1;
                                order.Status = newStatus;

                                using (var updateCmd = conn.CreateCommand())
                                {
                                    updateCmd.CommandText = "UPDATE orders SET order_status = @status WHERE order_id = @id";
                                    updateCmd.Parameters.AddWithValue("@status", newStatus ? 1 : 0);
                                    updateCmd.Parameters.AddWithValue("@id", order.OrderId);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<SanPhamHoaDon> GetItemOrder(string orderId)
        {
            List<SanPhamHoaDon> itemOrders = new();
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var query = @"
                    SELECT oi.product_id, oi.quantity, oi.price_at_order, p.name
                    FROM orders_items oi
                    JOIN products p ON oi.product_id = p.product_id
                    WHERE oi.order_id = @orderId";
                var cmd = DatabaseConnection.CreateCommand(query, conn);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var itemOrder = new SanPhamHoaDon
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            name = reader["name"].ToString(),
                            quantity = Convert.ToInt32(reader["quantity"]),
                            price_at_order = Convert.ToDecimal(reader["price_at_order"])
                        };
                        itemOrders.Add(itemOrder);
                    }
                }
            }
            return itemOrders;
        }

        private static StackPanel CreateRow(string label, UIElement control)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        Width = 150,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold
                    },
                    control
                }
            };
        }

        private static TextBlock CreateCell(string text, int row, int column, bool isHeader = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(2)
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, column);
            return tb;
        }

        public Dathang dathangWindow;
        public void AddButton_Click(object sender, RoutedEventArgs e)
        {

            if (dathangWindow == null)
            {
                dathangWindow = new Dathang(UId, this.XamlRoot,homeWindow);
                dathangWindow.Closed += DathangWindow_Closed;
                dathangWindow.Activate();
            }
            else
            {
                dathangWindow.Activate(); // đã mở thì chỉ đưa lên
            }
        }
        private void DathangWindow_Closed(object sender, WindowEventArgs e)
        {
            dathangWindow = null;
        }
        private void DataGridHoaDon_AutoGeneratingColumn(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Cancel = true; // Không cho phép sinh thêm bất kỳ cột nào
        }


        private void Home_Closing(object sender, WindowEventArgs args)
        {
            if (dathangWindow != null)
            {
                dathangWindow.Close();
                dathangWindow = null; // Đặt lại để không giữ tham chiếu
            }
        }
    }
}
