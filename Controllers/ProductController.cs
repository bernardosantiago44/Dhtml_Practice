using Microsoft.AspNetCore.Mvc;
using Dhtmlx_Practice.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Serilog;

namespace Dhtmlx_Practice.Controllers
{
    public class ProductController : Controller
    {
        private readonly IConfiguration _configuration;
        private string _connectionString; 

        public ProductController(IConfiguration configuration)
        {
            _configuration = configuration;
            try
            {
                SetConnectionString();
            } catch(ArgumentException e)
            {
                Log.Error(e.Message);
            }
        }

        private void SetConnectionString()
        {
            var connection = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connection))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not configured.");
            }
            _connectionString = connection;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            var products = await GetPagedProductAsync(page, pageSize);
            int totalRecords = await GetTotalProductCountAsync();

            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(products);
        }

        private async Task<List<Product>> GetPagedProductAsync(int page, int nextItems)
        {
            List<Product> products = new List<Product>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT Id, Description, Price
                FROM Products 
                ORDER BY Id 
                OFFSET @Offset ROWS 
                FETCH NEXT @PageSize ROWS ONLY;";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Offset", (page - 1) * nextItems));
            command.Parameters.Add(new SqlParameter("@PageSize", nextItems));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                products.Add(new Product {
                    Id = reader.GetInt32(0),
                    Description = reader.GetString(1),
                    Price = reader.GetDecimal(2)
                });
            }


            return products;
        }

        private async Task<int> GetTotalProductCountAsync()
        {
            int count = 0;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM Products";

            using var command = new SqlCommand(query, connection);
            count = (int) await command.ExecuteScalarAsync();

            return count;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            Console.WriteLine(">>> POST Create reached with: " + product.Description + ", " + product.Price);

            if (!ModelState.IsValid)
            {
                return View("Index", new List<Product>());
            }

            using var connection = new SqlConnection(_connectionString);
            var query = "INSERT INTO Products (Description, Price) VALUES (@description, @price)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@description", product.Description);
            command.Parameters.AddWithValue("@price", product.Price);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
