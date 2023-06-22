using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Http
{
    public class ClientHelper
    {
        private HttpClient httpClient;

        public ClientHelper()
        {
            httpClient = new HttpClient();
        }

        public async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                AddHeaders(request, headers);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                T result = JsonConvert.DeserializeObject<T>(content);

                return result;
            }
        }

        public async Task<T> PostAsync<T>(string url, object data, Dictionary<string, string> headers = null)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                AddHeaders(request, headers);
                string json = JsonConvert.SerializeObject(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                T result = JsonConvert.DeserializeObject<T>(content);

                return result;
            }
        }

        public async Task<(bool Success, string Response)> PostAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                AddHeaders(request, headers);
                string json = JsonConvert.SerializeObject(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    return (true, content);
                }
                else
                {
                    string errorMessage = response.ReasonPhrase;
                    return (false, errorMessage);
                }
            }
        }

        private void AddHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
        {
            httpClient.DefaultRequestHeaders.Clear();

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }
    }
}
