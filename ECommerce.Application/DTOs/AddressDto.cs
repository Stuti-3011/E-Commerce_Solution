namespace ECommerce.Application.DTOs
{
    public class AddressDto
    {
        public string RecipientName { get; set; }
        public string Phone { get; set; }
        public string AddressLine { get; set; }
        public string City { get; set; }
        public string Pincode { get; set; }
        public bool IsDefault { get; set; }
    }
}
