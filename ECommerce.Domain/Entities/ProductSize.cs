using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ECommerce.Domain.Entities
{
    public class ProductSize
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Size { get; set; }
        public int StockQuantity { get; set; }
        public int DisplayOrder { get; set; }
        [JsonIgnore]
        public Product Product { get; set; }
    }
}
