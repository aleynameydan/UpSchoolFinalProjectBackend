using Application.Common.Models;
using Application.Features.OrderEvents.Commands.Add;
using Application.Features.Orders.Commands.Add;
using Application.Features.Products.Commands.Add;
using Domain.Dtos;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

bool Continue = false;

using var httpClient = new HttpClient();

List<Product> productsList = new List<Product>();

var hubConnection = new HubConnectionBuilder()
    .WithUrl($"https://localhost:5275/Hubs/SeleniumLogHub")
    .WithAutomaticReconnect()
    .Build();

await hubConnection.StartAsync();

SeleniumLogDto CreateLog(string message) => new SeleniumLogDto(message);

while (!Continue)
{
    // Kullanıcı internet bağlantısı kontrolü

    if (!NetworkInterface.GetIsNetworkAvailable())
    {
        Console.WriteLine("İnternet bağlantısı bulunamadı. Kazıma işlemi başlatılamıyor.");
        continue;
    }

    // Kullanıcı tercihleri
    Console.WriteLine("Kaç tane ürün kazımak istiyorsunuz? (Bir sayı verebilir veya 'hepsi' yazabilirsiniz.)");
    var requestedAmount = Console.ReadLine();

    Console.WriteLine("Hangi tipteki ürünleri kazımak istiyorsunuz?");
    Console.WriteLine("A-) Hepsi");
    Console.WriteLine("B-) İndirimli");
    Console.WriteLine("C-) Normal Fiyatlı");
    var productCrawlType = Console.ReadLine();


    Console.WriteLine("Uygulama sonucunda kazınan ürünlerin tarafınıza e-mail yolu ile excel dosyası halinde aktarılmasını istiyor musunuz? Y/N");
    var sendtToEmail = Console.ReadLine();


    await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog("Kullanıcı tercihleri alındı."));


    var orderAddRequest = new OrderAddCommand();

    await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog("Order isteği yapıldı."));

    bool userPreferences = false;

    while (!userPreferences)
    {

        switch (productCrawlType.ToUpper())
        {
            case "A":
                orderAddRequest = new OrderAddCommand()
                {
                    Id = Guid.NewGuid(),
                    ProductCrawlType = ProductCrawlType.All,
                    CreatedOn = DateTimeOffset.Now,

                };
                userPreferences = true;
                break;
            case "B":
                orderAddRequest = new OrderAddCommand()
                {
                    Id = Guid.NewGuid(),
                    ProductCrawlType = ProductCrawlType.OnDiscount,
                    CreatedOn = DateTimeOffset.Now,
                };
                userPreferences = true;
                break;
            case "C":
                orderAddRequest = new OrderAddCommand()
                {
                    Id = Guid.NewGuid(),
                    ProductCrawlType = ProductCrawlType.NonDiscount,
                    CreatedOn = DateTimeOffset.Now,
                };
                userPreferences = true;
                break;
            default:
                Console.WriteLine("Geçersiz seçenek");
                Thread.Sleep(1500);
                Console.Clear();
                break;

                await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog(OrderStatus.CrawlingFailed.ToString()));
        }
    }

    var orderAddResponse = await SendHttpPostRequest<OrderAddCommand, object>(httpClient, "https://localhost:5275/api/Orders/Add", orderAddRequest);
    Guid orderId = orderAddRequest.Id;


    // Ayarlar ve yönlendirme
    ChromeOptions options = new ChromeOptions();
    options.AddArgument("--start-maximized");
    options.AddArgument("--disable-notifications");
    options.AddArgument("--disable-popup-blocking");

    var Driver = new ChromeDriver(options);

    var Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

    Console.Clear();

    Driver.Navigate().GoToUrl("https://finalproject.dotnet.gg/");

    var orderEventAddRequest = new OrderEventAddCommand()
    {
        OrderId = orderId,
        Status = OrderStatus.BotStarted,
    };

    var orderEventAddResponse = await SendHttpPostRequest<OrderEventAddCommand, object>(httpClient, "https://localhost:5275/api/OrderEvents/Add", orderEventAddRequest);

    //await SendLogNotification(orderEventAddRequest.Status.ToString());

    await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog(OrderStatus.BotStarted.ToString()));

    IWebElement pageCountElement = Wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector(".pagination > li:nth-last-child(2) > a")));
   
    int pageCount = int.Parse(pageCountElement.Text);

    Console.WriteLine($"{pageCount} adet sayfa mevcut.");
    Console.WriteLine("---------------------------------------");

    int itemCount = 0;

    orderEventAddRequest = new OrderEventAddCommand()
    {
        OrderId = orderId,
        Status = OrderStatus.CrawlingStarted,
    };

    orderEventAddResponse = await SendHttpPostRequest<OrderEventAddCommand, object>(httpClient, "https://localhost:5275/api/OrderEvents/Add", orderEventAddRequest);

    await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog(OrderStatus.CrawlingStarted.ToString()));

    for (int i = 1; i <= pageCount; i++)
    {
        Driver.Navigate().GoToUrl($"https://finalproject.dotnet.gg/?currentPage={i}");

        Console.WriteLine($"{i}. Sayfa");

        WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

        Thread.Sleep(500);

        IReadOnlyCollection<IWebElement> productElements = Driver.FindElements(By.CssSelector(".card.h-100"));

        

        foreach (IWebElement productElement in productElements)
        {
            

            bool includeProduct = false;

            if (productCrawlType.ToUpper() == "A") // Hepsi seçeneği
            {
                includeProduct = true;
            }
            else if (productCrawlType.ToUpper() == "B") // İndirimli ürünler seçeneği
            {
                if (productElement.FindElements(By.CssSelector(".sale-price")).Any())
                    includeProduct = true;
            }
            else if (productCrawlType.ToUpper() == "C") // Normal fiyatlı ürünler seçeneği
            {
                if (!productElement.FindElements(By.CssSelector(".sale-price")).Any())
                    includeProduct = true;
            }

            if (includeProduct)
            {
                // Kullanıcının istediği sayıda ürün al
                if (requestedAmount.ToLower() == "hepsi" || itemCount < int.Parse(requestedAmount))
                {
                    string productName = productElement.FindElement(By.CssSelector(".fw-bolder.product-name")).GetAttribute("innerText");

                    string productPrice = productElement.FindElement(By.CssSelector(".price")).GetAttribute("innerText");

                    productPrice = productPrice.Replace("$", "").Replace(",", "").Trim();

                    decimal price = string.IsNullOrWhiteSpace(productPrice) ? 0 : decimal.Parse(productPrice, CultureInfo.InvariantCulture);

                    string productSalePrice = string.Empty;

                    IWebElement salePriceElement = null;

                    try
                    {
                        salePriceElement = productElement.FindElement(By.CssSelector(".sale-price"));
                    }
                    catch (NoSuchElementException)
                    {
                        // .sale-price öğesi bulunamadı, ürünün indirimli fiyatı yok
                    }

                    decimal salePrice = 0;

                    if (salePriceElement != null)
                    {
                        productSalePrice = salePriceElement.GetAttribute("innerText");

                        productSalePrice = productSalePrice.Replace("$", "").Replace(",", "").Trim();

                        salePrice = string.IsNullOrWhiteSpace(productSalePrice) ? 0 : decimal.Parse(productSalePrice, CultureInfo.InvariantCulture);
                    }

                    bool isOnSale = productElement.FindElements(By.CssSelector(".sale-price")).Count > 0;

                    string pictureUrl = productElement.FindElement(By.CssSelector(".card-img-top")).GetAttribute("src");

                    Console.WriteLine("Ürün Adı: " + productName);
                    Console.WriteLine("İndirimli mi?: " + isOnSale);

                    if (isOnSale)
                    {
                        Console.WriteLine("İndirimli Fiyat: " + salePrice);
                    }
                    else
                    {
                        Console.WriteLine("İndirimli Fiyat: İndirim yok");
                    }

                    Console.WriteLine("İndirimsiz Fiyat: " + productPrice);
                    Console.WriteLine("Ürün Resmi URL'si: " + pictureUrl);
                    Console.WriteLine("----------------------------");

                    var productAddRequest = new ProductAddCommand()
                    {

                        OrderId = orderAddRequest.Id,
                        Name = productName,
                        Picture = pictureUrl,
                        IsOnSale = isOnSale,
                        Price = price,
                        SalePrice = salePrice,
                        CreatedOn = DateTimeOffset.Now

                     };

                    var product = new Product()
                    {

                        OrderId = orderAddRequest.Id,
                        Name = productName,
                        Picture = pictureUrl,
                        IsOnSale = isOnSale,
                        Price = price,
                        SalePrice = salePrice,
                        CreatedOn = DateTimeOffset.Now

                    };

                    var productAddResponse = await SendHttpPostRequest<ProductAddCommand, object>(httpClient, "https://localhost:5275/api/Products/Add", productAddRequest);

                    productsList.Add(product);

                    itemCount++;
                }
            }
         
        }

        if (sendtToEmail.ToUpper() == "Y")
        {

            Console.WriteLine("Lütfen geçerli bir e-mail adresi giriniz!");
            var userEmail = Console.ReadLine();

            ExcelModel excelModel = new ExcelModel();

            string recipientEmail = userEmail;

            string subject = "Excel dosyası";

            string body = "Merhaba, ekteki Excel dosyasında ürünler bulunmaktadır.";

            excelModel.ExportToExcel(productsList, recipientEmail, subject, body);
        }
        else if (sendtToEmail.ToUpper() == "N")
        {
            continue;
        }
        else
        {
            Console.WriteLine("Geçersiz giriş!");
            continue;
        }
    }

    Console.WriteLine($"{itemCount} adet ürün bulundu.");


    orderEventAddRequest = new OrderEventAddCommand()
    {
        OrderId = orderId,
        Status = OrderStatus.CrawlingCompleted,
    };

    orderEventAddResponse = await SendHttpPostRequest<OrderEventAddCommand, object>(httpClient, "https://localhost:5275/api/OrderEvents/Add", orderEventAddRequest);

    await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog(OrderStatus.CrawlingCompleted.ToString()));

    orderEventAddRequest = new OrderEventAddCommand()
    {
        OrderId = orderId,
        Status = OrderStatus.OrderCompleted,
    };

    orderEventAddResponse = await SendHttpPostRequest<OrderEventAddCommand, object>(httpClient, "https://localhost:5275/api/OrderEvents/Add", orderEventAddRequest);

    await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog(OrderStatus.OrderCompleted.ToString()));

    Console.WriteLine("Kazıma işlemine devam etmek istiyor musunuz? (Y/N)");

    var choiceContinue = Console.ReadLine();

    if (choiceContinue.ToUpper()=="Y")
    {
        Driver.Dispose();

        await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog("Bot Yeniden Başlatılacak!"));
    }
    else if (choiceContinue.ToUpper() == "N")
    {
        Driver.Dispose();

        httpClient.Dispose();

        Continue = true;

        await hubConnection.InvokeAsync("SendLogNotificationAsync", CreateLog("Bot Durduruldu!"));
    }
}

async Task<TResponse> SendHttpPostRequest<TRequest, TResponse>(HttpClient httpClient, string url, TRequest payload)
{
    var jsonPayload = JsonConvert.SerializeObject(payload);
    var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync(url, httpContent);
    response.EnsureSuccessStatusCode();

    var jsonResponse = await response.Content.ReadAsStringAsync();
    var responseObject = JsonConvert.DeserializeObject<TResponse>(jsonResponse);
    //Console.WriteLine($"Response: {responseObject}");

    return responseObject;
}

async Task SendLogNotification(string logMessage)
{
    // 'CreateLog' metodu burada kullanılarak bir günlük oluşturulabilir
    var log = CreateLog(logMessage);

    // HubConnection oluşturulmalı ve başlatılmalı
    var hubConnection = new HubConnectionBuilder()
        .WithUrl("https://localhost:5275/Hubs/SeleniumLogHub") // Hub URL'sini burada belirtmelisiniz
        .Build();

    try
    {
        await hubConnection.StartAsync(); // HubConnection'ı başlatma
        await hubConnection.InvokeAsync("SendLogNotificationAsync", log); // Metodu çağırma
    }
    finally
    {
        await hubConnection.DisposeAsync(); // HubConnection'ı kapatma ve kaynakları temizleme
    }
}