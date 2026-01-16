using System.Collections.Generic;
using System.Linq;

namespace Zoolirante_Open_Minded.ViewModels
{
    public class CartVM
    {
        public List<CartItemVM> Items { get; set; } = new();
        public int ItemCount => Items.Sum(i => i.Qty);
        public decimal Subtotal => Items.Sum(i => i.LineTotal);
        public int? PickupLocationId { get; set; }
        public string? PickupLocationName { get; set; }
		public decimal TotalSavings => Items.Sum(i => i.LineSavings);
	}
}


