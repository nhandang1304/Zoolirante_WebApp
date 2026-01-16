namespace Zoolirante_Open_Minded.Models
{
    public class EventAnimal
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Animals { get; set; }

        public virtual Event Event { get; set; }
    }

}
