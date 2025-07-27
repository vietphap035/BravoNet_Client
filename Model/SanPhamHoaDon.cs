using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BravoNet_Client.Model
{
    public class SanPhamHoaDon : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string img { get; set; }
        public string name { get; set; }

        private int _quantity;
        public int quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(quantity));
                }
            }
        }
        public int quantity_at_order { get; set; }

        public decimal price_at_order { get; set; }
        public int TonKho { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
