using BravoNet_Client.Model;
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
using System.Collections.ObjectModel;
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
    public sealed partial class Dathang : Window
    {
        public string UId { get; set; }
        public List<SanPham> sanPhams { get; set; } = new();
        private Home homeWindow;
        public ObservableCollection<SanPhamHoaDon> ProductList { get; set; } = new();
        private XamlRoot xamlRoot;
        public Dathang(string UId, XamlRoot xaml, Home home)
        {
            InitializeComponent();
            this.UId = UId;
            this.xamlRoot = xaml;
            this.homeWindow = home;
            ProductList = new ObservableCollection<SanPhamHoaDon>(LoadSanPham());
        }
        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SanPhamHoaDon sp)
            {
                sp.quantity++;
            }
        }

        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SanPhamHoaDon sp)
            {
                if (sp.quantity > 0)
                    sp.quantity--;
            }
        }

        public void Huy_btn(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public async void XacNhan_btn(object sender, RoutedEventArgs e)
        {
            var selectedProducts = ProductList.Where(p => p.quantity > 0).ToList();

            if (selectedProducts.Any())
            {
                // Kiểm tra số lượng đặt > tồn kho
                var invalidProduct = selectedProducts.FirstOrDefault(p => p.quantity > p.quantity_at_order);
                if (invalidProduct != null)
                {
                    await new ContentDialog
                    {
                        Title = "Lỗi số lượng",
                        Content = $"Sản phẩm '{invalidProduct.name}' chỉ còn {invalidProduct.quantity_at_order} trong kho.",
                        CloseButtonText = "OK",
                        XamlRoot = this.xamlRoot
                    }.ShowAsync();
                    return;
                }

                // Xác nhận đặt hàng
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Xác nhận đặt hàng",
                    Content = "Bạn có chắc chắn muốn đặt hàng không?",
                    PrimaryButtonText = "Đặt hàng",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.xamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // TODO: Lưu đơn hàng vào DB

                    using (var conn = DatabaseConnection.GetConnection())
                    {
                        conn.Open();
                        var orderId = Guid.NewGuid().ToString(); // Tạo ID đơn hàng ngẫu nhiên
                        var query = "INSERT INTO orders (order_id, UId, order_date, order_status) VALUES (@orderId, @UId, @orderDate, @status)";
                        var cmd = DatabaseConnection.CreateCommand(query, conn);
                        cmd.Parameters.AddWithValue("@orderId", orderId);
                        cmd.Parameters.AddWithValue("@UId", this.UId);
                        cmd.Parameters.AddWithValue("@orderDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@status", false); // Trạng thái đơn hàng là đã đặt
                        cmd.ExecuteNonQuery();
                        foreach (var product in selectedProducts)
                        {
                            var detailQuery = "INSERT INTO orders_items (order_id, product_id, quantity, price_at_order) VALUES (@orderId, @productId, @quantity, @price)";
                            var detailCmd = DatabaseConnection.CreateCommand(detailQuery, conn);
                            detailCmd.Parameters.AddWithValue("@orderId", orderId);
                            detailCmd.Parameters.AddWithValue("@productId", product.Id);
                            detailCmd.Parameters.AddWithValue("@quantity", product.quantity);
                            detailCmd.Parameters.AddWithValue("@price", product.price_at_order);
                            detailCmd.ExecuteNonQuery();
                        }
                    }
                    await new ContentDialog
                    {
                        Title = "Đặt hàng thành công",
                        Content = $"Đã đặt {selectedProducts.Count} sản phẩm.",
                        CloseButtonText = "OK",
                        XamlRoot = this.xamlRoot
                    }.ShowAsync();
                    homeWindow.LoadHoaDon();
                    this.Close();
                }
            }
            else
            {
                await new ContentDialog
                {
                    Title = "Không có sản phẩm nào được chọn",
                    Content = "Vui lòng chọn ít nhất một sản phẩm để đặt hàng.",
                    CloseButtonText = "OK",
                    XamlRoot = this.xamlRoot
                }.ShowAsync();
            }
        }


        public List<SanPhamHoaDon> LoadSanPham()
        {
            List<SanPhamHoaDon> sanPhamList = new();
            using(var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                var query = "SELECT product_id, imgPath, name, quantity, price FROM products";
                var cmd = DatabaseConnection.CreateCommand(query, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var sanPham = new SanPhamHoaDon
                        {
                            Id = reader.GetInt32("product_id"),
                            img  = reader["imgPath"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("imgPath")) : "Assets/StoreLogo.png",
                            name = reader.GetString("name"),
                            quantity = 0,
                            price_at_order = reader.GetDecimal("price"),
                            quantity_at_order = reader.GetInt32("quantity")
                        };
                        sanPhamList.Add(sanPham);
                        
                    }
                }
            }
            return sanPhamList;
        }
    }
}
