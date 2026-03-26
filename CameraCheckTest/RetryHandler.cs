namespace CameraCheckTest
{
    public class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 3;

        public RetryHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    Console.WriteLine($"Отправка HTTP Request попытка - {i}");
                    response = await base.SendAsync(request, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Отправка HTTP Request ошибка - {ex.Message}");
                }
            }

            return response;
        }
    }
}
