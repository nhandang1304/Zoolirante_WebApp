namespace Zoolirante_Open_Minded.Models
{
	public class Membership
	{
		public int MembershipId { get; set; }
		public int UserId { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public bool IsActive => EndDate >= DateTime.Now;
        public User User { get; set; }
    }
}
