using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Threading;
using System.Collections.Concurrent;
using Mitsu3E;
using ASSY_CALLARM;

namespace ASSY_CALLAMR
{
    // Mẫu phản hồi API (có thể thay đổi)
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public JToken Data { get; set; } // Linh hoạt như JsonElement
    }

    public class EquipmentApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<APIMessage> _queue = new ConcurrentQueue<APIMessage>();
        private readonly Task _workerTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed = false;

        // Bệ Hạ chọn: 1 = tuần tự, 5 = tối đa 5 cùng lúc
        public EquipmentApiService(string baseUrl, int maxConcurrent = 1)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            _workerTask = Task.Run(ProcessQueueAsync);
        }


        public void EnqueueRequest(APIMessage apiMsg)
        {
            if (apiMsg == null) throw new ArgumentNullException(nameof(apiMsg));
            _queue.Enqueue(apiMsg);
        }
        private async Task ProcessQueueAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out APIMessage item))
                {
                    LogApp.Info($"[LOG] Gửi API cho {item.KeyNo} | Tin nhắn: {item.Message}");

                    var result = await SendAsync(item);
                    item.Callback?.Invoke(result);
                }
                else
                {
                    await Task.Delay(50, _cts.Token);
                }
            }
        }

        private async Task<TaskResponse> SendAsync(APIMessage mess)
        {
            await _semaphore.WaitAsync(_cts.Token);
            try
            {
                // 1. Đặt Header: KeyNo
                _httpClient.DefaultRequestHeaders.Remove("KeyNo");
                _httpClient.DefaultRequestHeaders.Add("KeyNo", mess.KeyNo);

                // 2. Tạo Body: {} (rỗng nhưng BẮT BUỘC PHẢI CÓ)
                var emptyJsonBody = string.IsNullOrEmpty( mess.Message )?"{}": mess.Message; 
                var content = new StringContent(emptyJsonBody, Encoding.UTF8, "application/json");

                // 3. Gửi POST
                var response = await _httpClient.PostAsync("api/v1/equipment/task", content, _cts.Token);
                var responseJson = await response.Content.ReadAsStringAsync();

                // 4. Xử lý phản hồi
                if (!response.IsSuccessStatusCode)
                {
                    return new TaskResponse
                    {
                        ResultCode = (int)response.StatusCode,
                        ResultMessage = $"HTTP Error: {responseJson}"
                    };
                }

                return JsonConvert.DeserializeObject<TaskResponse>(responseJson)
                       ?? new TaskResponse { ResultCode = -1, ResultMessage = "JSON parse failed" };
            }
            catch (Exception ex)
            {
                return new TaskResponse { ResultCode = -1, ResultMessage = ex.Message };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cts.Cancel();
                _cts.Dispose();
                _httpClient?.Dispose();
                _semaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}
