﻿#pragma warning disable SKEXP0003
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0011
#pragma warning disable SKEXP0040
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0052

using Microsoft.EntityFrameworkCore;
using SearchEntities;
using DataEntities;
using Products.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;

namespace Products.Memory;

public class MemoryContext
{
    const string MemoryCollectionName = "products";

    private ILogger _logger;
    private IConfiguration _config;
    private ChatHistory _chatHistory;
    public Kernel _kernel;
    private IChatCompletionService _chat;
    public ISemanticTextMemory _memory;

    public void InitMemoryContext(ILogger logger, IConfiguration config, ProductDataContext db)
    {
        _logger = logger;

        // get the configuration settings
        var chatDeploymentName = config["AZURE_OPENAI_MODEL"];
        var endpoint = config["AZURE_OPENAI_ENDPOINT"];
        var apiKey = config["AZURE_OPENAI_APIKEY"];
        var ada002 = config["AZURE_OPENAI_ADA02"];

        // create kernel and add chat completion
        var builderSK = Kernel.CreateBuilder().
        AddAzureOpenAIChatCompletion(chatDeploymentName, endpoint, apiKey);
        _kernel = builderSK.Build();
        _chat = _kernel.GetRequiredService<IChatCompletionService>();

        // create Semantic Memory
        var memoryBuilder = new MemoryBuilder();
        memoryBuilder.WithAzureOpenAITextEmbeddingGeneration(ada002, endpoint, apiKey);
        memoryBuilder.WithMemoryStore(new VolatileMemoryStore());
        _memory = memoryBuilder.Build();

        // create chat history
        _chatHistory = new ChatHistory();

        FillProductsAsync(db);
    }

    public async Task FillProductsAsync(ProductDataContext db)
    {
        // get a copy of the list of products
        var products = await db.Product.ToListAsync();

        // iterate over the products and add them to the memory
        foreach (var product in products)
        {
            var productInfo = $"[{product.Name}] is a product that costs [{product.Price}] and is described as [{product.Description}]";

            await _memory.SaveInformationAsync(
                collection: MemoryCollectionName,
                text: productInfo,
                id: product.Id.ToString(),
                description: product.Description);
        }
    }

    public async Task<SearchResponse> Search(string search, ProductDataContext db)
    {
        var response = new SearchResponse();
        Product firstProduct = null;
        var responseText = "";

        // search the vector database for the most similar product        
        var memorySearchResult = await _memory.SearchAsync(MemoryCollectionName, search).FirstOrDefaultAsync();
        if (memorySearchResult != null && memorySearchResult.Relevance > 0.8)
        {
            // product found, search the db for the product details
            var prodId = memorySearchResult.Metadata.Id;
            firstProduct = await db.Product.FindAsync(int.Parse(prodId));
            if (firstProduct != null)
            {
                responseText = $"The product [{firstProduct.Name}] fits with the search criteria [{search}]";
            }
        }

        if (firstProduct == null)
        {
            // no product found, ask the AI, keep the chat history
            _chatHistory.AddUserMessage(search);
            var result = await _chat.GetChatMessageContentsAsync(_chatHistory);
            responseText = result[^1].Content;
            _chatHistory.AddAssistantMessage(responseText);
        }

        // create a response object
        return new SearchResponse
        {
            Products = firstProduct == null ? [new Product()] : [firstProduct],
            Response = responseText
        };
    }
}

public static class Extensions
{
    public static void InitSemanticMemory(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<MemoryContext>();
        context.InitMemoryContext(
            services.GetRequiredService<ILogger<ProductDataContext>>(),
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<ProductDataContext>());
    }
}
