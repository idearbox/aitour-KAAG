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

public class AIMemoryContext
{
    const string PRODUCT_COLLECTION_NAME = "products";

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
        //"AZURE_OPENAI_MODEL": "gpt-4o",
        //"AZURE_OPENAI_ADA02": "text-embedding-ada-002"
        var ai_model1 = config["AZURE_OPENAI_MODEL"];
        var ai_model2 = config["AZURE_OPENAI_ADA02"];
        var endpoint = config["AZURE_OPENAI_ENDPOINT"];
        var apiKey = config["AZURE_OPENAI_APIKEY"];

        //2.. Semantic Kernel 생성
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
                collection: PRODUCT_COLLECTION_NAME,
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
        _chatHistory.Clear();

        #region 1..vector database 에서 유사 상품 조회
        var memorySearchResult = await _memory.SearchAsync(PRODUCT_COLLECTION_NAME, search).FirstOrDefaultAsync();
        if (memorySearchResult != null && memorySearchResult.Relevance > 0.6)
        {
            // product found, search the db for the product details
            var prodId = memorySearchResult.Metadata.Id;
            firstProduct = await db.Product.FindAsync(int.Parse(prodId));
            if (firstProduct != null)
            {
                responseText = $">>..The product [{firstProduct.Name}] fits with the search criteria [{search}][{memorySearchResult.Relevance.ToString("0.00")}] <<\n\r";
            }
        }

        if (firstProduct == null)
        {
            responseText = $">>..no product found..<<\n\r";
        }
        #endregion

        #region  2..생성형 응답 구성
        string p;
        // 상품조회 => ask the AI, chat history 구성
        #region 2-1..생성형 응답 구성1
        //p = search;
        #endregion

        #region 2-2..생성형 응답 구성2
        p = @$"  You are an intelligent assistant helping eShop Inc clients with their search about outdoor products.
                                Use 'you' to refer to the individual asking the questions even if they ask with 'I'.";
        if (firstProduct != null)
        {
            p += @$"Answer the questions using only the data provided related to a product in the response below. 
                            always response in korean.
                            Do not include the product id.
                            Do not return markdown format. Do not return HTML format.

                            As the assistant, you generate descriptions using a funny style and even add some personal flair with appropriate emojis.

                            Generate and answer to the question using the information below.
                            Incorporate the question if provided: {search}
                            Always incorporate the product name, description, and price in the response.
                            If you cannot answer using the information below, say you don't know.
                            
                            +++++
                    product id: {firstProduct.Id}
                    product name: {firstProduct.Name}
                    product description: {firstProduct.Description}
                    product price: {firstProduct.Price}
                            +++++";
            ////            //product id: { firstProduct.Id}
            ////            //product name: { firstProduct.Name}
            ////            //product description: { firstProduct.Description}
            ////            //product price: { firstProduct.Price}

        }
        else
        {
            p += @$"If no products are found, respond in a polite, encouraging, and engaging manner. 
                                Add a little humor or emojis to keep the response friendly and approachable.

                                Generate a response for when no products match the search. For example:
                                'Hmm, it seems we don't have what you're looking for right now! 🤔
                                 Why not try searching for something else? 
                                 Or tell me more about what you need, and I can assist you better! 🙌'";
        }
        //p += @" and response in korean.";
        #endregion

        _chatHistory.AddUserMessage(p);
        var result = await _chat.GetChatMessageContentsAsync(_chatHistory);
        responseText += result[^1].Content + "\n\r";

        _chatHistory.AddAssistantMessage(responseText);
        #endregion

        #region 3..결과 반환 
        return new SearchResponse
        {
            Products = firstProduct == null ? [new Product()] : [firstProduct],
            Response = responseText + ">>..done..<<"
        };
        #endregion
    }
}

public static class Extensions
{
    public static void InitSemanticMemory(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AIMemoryContext>();
        context.InitMemoryContext(
            services.GetRequiredService<ILogger<ProductDataContext>>(),
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<ProductDataContext>());
    }
}
