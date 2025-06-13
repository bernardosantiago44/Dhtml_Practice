using Microsoft.AspNetCore.Mvc;

namespace Dhtmlx_Practice.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } // Cambiar por double
    }
}

