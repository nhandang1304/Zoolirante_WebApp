using System.ComponentModel.DataAnnotations;
namespace Zoolirante_Open_Minded.ViewModels
{
	public class CardPaymentVM
	{
		[Required]
		public int OrderId { get; set; }

		public string? Purpose { get; set; }
		public int? TicketId { get; set; }          
		public int? Qty { get; set; }                
		public DateTime? VisitDate { get; set; }    

		[Required, StringLength(120)]
		[Display(Name = "Name on card")]
		public string CardholderName { get; set; } = "";

		[Required, Display(Name = "Card number")]
		[RegularExpression(@"^\d{13,19}$", ErrorMessage = "Card number must be 13–19 digits")]
		public string CardNumber { get; set; } = "";

		[Required, Range(1, 12, ErrorMessage = "Month 1–12")]
		[Display(Name = "Exp. month")]
		public int ExpMonth { get; set; }

		[Required, Range(2024, 2045, ErrorMessage = "Year 2024–2045")]
		[Display(Name = "Exp. year")]
		public int ExpYear { get; set; }

		[Required, RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV 3–4 digits")]
		[Display(Name = "CVV")]
		public string Cvv { get; set; } = "";

		[StringLength(80)]
		[Display(Name = "Billing ZIP/Postal (optional)")]
		public string? BillingZip { get; set; }
		public string? TicketType { get; set; } = "Adult";
	}
}
