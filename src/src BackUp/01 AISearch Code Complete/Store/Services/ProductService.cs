using SearchEntities;
using DataEntities;
using System.Text.Json;

namespace Store.Services;

public class ProductService
{
    HttpClient httpClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
    {
        _logger = logger;
        this.httpClient = httpClient;
    }
    public async Task<List<Product>> GetProducts()
    {
        List<Product>? products = null;
        try
        {
            var response = await httpClient.GetAsync("/api/product");
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Http status code: {response.StatusCode}");
            _logger.LogInformation($"Http response content: {responseText}");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                products = await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.ListProduct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GetProducts.");
        }

        return products ?? new List<Product>();
    }

    public async Task<SearchResponse?> SearchDB(string searchTerm)
    {
        //DB 검색
        return await SearchCore(searchTerm, "/api/product/searchDB");
    }

    public async Task<SearchResponse?> SearchAI(string searchTerm)
    {
        //AI 검색
        return await SearchCore(searchTerm, "/api/product/searchAI");
    }

    public async Task<SearchResponse?> SearchCore(string searchTerm, string uri)
    {
        try
        {
            HttpResponseMessage response;
            response = await httpClient.GetAsync($"{uri}/{searchTerm}");
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Http status code: {response.StatusCode}");
            _logger.LogInformation($"Http response content: {responseText}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SearchResponse>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Search.");
        }

        return new SearchResponse { Response = "No response" };
    }
}
