#region pragma warning disable
#pragma warning disable SKEXP0003
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0011
#pragma warning disable SKEXP0040
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0052 
#endregion

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


        //1.. 설정값 가져오기
        var ai_model1 = config["AZURE_OPENAI_MODEL"];
        var ai_model2 = config["AZURE_OPENAI_ADA02"];
        var endpoint = config["AZURE_OPENAI_ENDPOINT"];
        var apiKey = config["AZURE_OPENAI_APIKEY"];

        //2.. 커널 빌드
        var builderSK = Kernel.CreateBuilder()
                              .AddAzureOpenAIChatCompletion(ai_model1, endpoint, apiKey);
        _kernel = builderSK.Build();
        _chat = _kernel.GetRequiredService<IChatCompletionService>();

        //3.. Semantic Memory 생성
        var memoryBuilder = new MemoryBuilder();
        memoryBuilder.WithAzureOpenAITextEmbeddingGeneration(ai_model2, endpoint, apiKey);
        memoryBuilder.WithMemoryStore(new VolatileMemoryStore());
        _memory = memoryBuilder.Build();

        //4..chat history 생성
        _chatHistory = new ChatHistory();

        //5..상품 목록 조회=>Memory 저장
        FillProductsAsync(db);
    }

    public async Task FillProductsAsync(ProductDataContext db)
    {
        //get 상품목록
        var products = await db.Product.ToListAsync();

        //상품 Semantic Memory에 저장
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

        //1..vector database 에서 유사 상품 조회
        var memorySearchResult = await _memory.SearchAsync(MemoryCollectionName, search).FirstOrDefaultAsync();
        if (memorySearchResult != null && memorySearchResult.Relevance > 0.6)
        {
            // product found, search the db for the product details
            var prodId = memorySearchResult.Metadata.Id;
            firstProduct = await db.Product.FindAsync(int.Parse(prodId));
            if (firstProduct != null)
            {
                responseText = $"The product [{firstProduct.Name}] fits with the search criteria [{search}][{memorySearchResult.Relevance.ToString("0.00")}]";
            }
        }

        if (firstProduct == null)
        {
            // 상품조회 실패=> ask the AI, chat history 구성
            _chatHistory.AddUserMessage(search);
            var result = await _chat.GetChatMessageContentsAsync(_chatHistory);
            responseText = result[^1].Content;
            _chatHistory.AddAssistantMessage(responseText);
        }
        else
        {
            #region 2..생성형 응답 구성, chat history 구성
            _chatHistory.Clear();
            //2..생성형 응답 구성, chat history 구성////            and response in korean
            string p = @$"
                    You are an intelligent assistant helping eShop Inc clients with their search about outdoor product.
                    Use 'you' to refer to the individual asking the questions even if they ask with 'I'.
                    Answer the questions using only the data provided related to a product in the response below. 
                    Do not include the product id.
                    Do not return markdown format. Do not return HTML format.
                    If you cannot answer using the information below, say you don't know. 
                    As the assistant, you generate descriptions using a funny style and even add some personal flair with appropriate emojis.

                    Generate and answer to the question using the information below.
                    Incorporate the question if provided: {search}
                    Always incorporate the product name, description, and price in the response.
                    +++++
                    product id: {firstProduct.Id}
                    product name: {firstProduct.Name}
                    product description: {firstProduct.Description}
                    product price: {firstProduct.Price}
                    +++++
            ";
            _chatHistory.AddUserMessage(p);
            var resultPromt = await _chat.GetChatMessageContentsAsync(_chatHistory);
            responseText = resultPromt[0].Content;
            #endregion
        }

        //3..결과 반환 
        return new SearchResponse
        {
            Products = firstProduct == null ? [new Product()] : [firstProduct],
            Response = responseText+"..done"
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
