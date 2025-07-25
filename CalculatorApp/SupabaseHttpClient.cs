using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Windows;

namespace CalculatorApp
{
    // 1. HTTP клиент для работы с Supabase API
    public static class SupabaseHttpClient
    {
        // ЗАМЕНИТЕ НА ВАШИ ДАННЫЕ ИЗ SUPABASE DASHBOARD
        public const string SUPABASE_URL = "https://krszxihgewwrawskrpco.supabase.co";
        public const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imtyc3p4aWhnZXd3cmF3c2tycGNvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTMyNjYyNDYsImV4cCI6MjA2ODg0MjI0Nn0.yJYHLORsTPuS0vHKPZEcOQ6g6bfXpYBhZKa7jtN17zg";

        private static readonly HttpClient _httpClient = new HttpClient();
        public static string _authToken = "";

        static SupabaseHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Add("apikey", SUPABASE_ANON_KEY);
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        public static void SetAuthToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        public static async Task<string> GetAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{SUPABASE_URL}/rest/v1/{endpoint}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {errorContent}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Ошибка сети: {ex.Message}");
            }
        }

        public static async Task<string> PostAsync(string endpoint, string json)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{SUPABASE_URL}/rest/v1/{endpoint}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {errorContent}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Ошибка сети: {ex.Message}");
            }
        }

        public static async Task<string> PutAsync(string endpoint, string json)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{SUPABASE_URL}/rest/v1/{endpoint}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {errorContent}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Ошибка сети: {ex.Message}");
            }
        }

        public static async Task<string> AuthSignInAsync(string email, string password)
        {
            var authData = new { email, password };
            var json = JsonConvert.SerializeObject(authData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Создаем отдельный HttpClient для аутентификации
            using (var authClient = new HttpClient())
            {
                authClient.DefaultRequestHeaders.Add("apikey", SUPABASE_ANON_KEY);
                var response = await authClient.PostAsync($"{SUPABASE_URL}/auth/v1/token?grant_type=password", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }

    // 2. Модели данных для Supabase (простые классы)
    public class AppData
    {
        public string id { get; set; }
        public string data_type { get; set; }
        public Dictionary<string, object> data_content { get; set; }
        public int version { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string created_by { get; set; }
    }

    public class UserProfile
    {
        public string id { get; set; }
        public string username { get; set; }
        public string role { get; set; }
        public bool is_admin { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class AuthResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public AuthUser user { get; set; }
    }

    public class AuthUser
    {
        public string id { get; set; }
        public string email { get; set; }
    }

    // 3. Менеджер для работы с Supabase
    public static class SupabasePriceManager
    {
        private static Dictionary<string, Dictionary<string, double>> _localPrices;
        private static int _currentVersion = 1;

        public static async Task InitializeAsync()
        {
            try
            {
                await LoadPricesFromSupabase();
            }
            catch (Exception ex)
            {
                // Логируем подробную ошибку для отладки
                var detailMessage = ex.InnerException != null ?
                    $"{ex.Message}\nВнутренняя ошибка: {ex.InnerException.Message}" :
                    ex.Message;

                MessageBox.Show($"Ошибка подключения к облаку. Работаем в локальном режиме.\n\nДетали ошибки: {detailMessage}",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Загружаем локальные цены как fallback
                PriceManager.LoadPrices();
            }
        }

        public static async Task<bool> LoadPricesFromSupabase()
        {
            try
            {
                // Сначала проверяем простой запрос для диагностики
                var testResponse = await SupabaseHttpClient.GetAsync("app_data?limit=1");

                // Теперь пытаемся получить данные о ценах
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.prices&order=version.desc&limit=1");

                if (string.IsNullOrEmpty(response))
                {
                    throw new Exception("Пустой ответ от сервера");
                }

                // Проверяем, что ответ содержит валидный JSON
                if (response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentVersion = appData.version;

                        // Преобразуем JSON обратно в нужный формат
                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonString);

                        // Обновляем локальный PriceManager
                        UpdateLocalPriceManager();

                        return true;
                    }
                }
                else
                {
                    throw new Exception($"Неожиданный формат ответа: {response}");
                }

                // Данных нет в облаке, создаем начальные данные
                return false;
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"Ошибка сетевого подключения: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                throw new Exception($"Ошибка обработки данных: {jsonEx.Message}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки цен из облака: {ex.Message}");
            }
        }

        public static async Task<bool> SavePricesToSupabase(Dictionary<string, Dictionary<string, double>> prices)
        {
            try
            {
                // Проверяем, есть ли права администратора
                if (!await IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentVersion + 1;

                // Используем правильную структуру данных для Supabase
                var appDataForSave = new
                {
                    data_type = "prices",
                    data_content = prices, // Отправляем напрямую как Dictionary
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(_currentUserId) ? null : _currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                // Сначала пытаемся обновить существующую запись
                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.prices", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentVersion = newVersion;
                        _localPrices = prices;

                        // Также сохраняем локально как backup
                        PriceManager.UpdatePrices(ConvertToLocalFormat(prices));
                        PriceManager.SavePrices();

                        return true;
                    }
                }
                catch
                {
                    // Если обновление не удалось, создаем новую запись
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);

                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentVersion = newVersion;
                        _localPrices = prices;

                        // Также сохраняем локально как backup
                        PriceManager.UpdatePrices(ConvertToLocalFormat(prices));
                        PriceManager.SavePrices();

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения цен в облако: {ex.Message}");
            }
        }



        private static List<PriceItem> ConvertToLocalFormat(Dictionary<string, Dictionary<string, double>> prices)
        {
            var items = new List<PriceItem>();
            foreach (var type in prices.Keys)
            {
                foreach (var thickness in prices[type].Keys)
                {
                    items.Add(new PriceItem
                    {
                        Type = type,
                        Thickness = thickness,
                        Price = prices[type][thickness]
                    });
                }
            }
            return items;
        }

        private static void UpdateLocalPriceManager()
        {
            if (_localPrices != null)
            {
                var localItems = ConvertToLocalFormat(_localPrices);
                PriceManager.UpdatePrices(localItems);
            }
        }

        public static string _currentUserId = "";

        public static async Task<bool> IsCurrentUserAdmin()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentUserId))
                    return false;

                var response = await SupabaseHttpClient.GetAsync($"user_profiles?id=eq.{_currentUserId}&select=is_admin");
                var profiles = JsonConvert.DeserializeObject<List<UserProfile>>(response);

                return profiles.Count > 0 && profiles[0].is_admin;
            }
            catch
            {
                return false;
            }
        }

        public static Dictionary<string, Dictionary<string, double>> GetLocalPrices()
        {
            return _localPrices ?? new Dictionary<string, Dictionary<string, double>>();
        }

        public static int GetCurrentVersion()
        {
            return _currentVersion;
        }
    }

    // 4. Менеджер аутентификации Supabase через HTTP
    public static class SupabaseAuthManager
    {
        private static AuthUser _currentUser = null;

        public static async Task<bool> SignInAsync(string email, string password)
        {
            try
            {
                var response = await SupabaseHttpClient.AuthSignInAsync(email, password);
                var authResponse = JsonConvert.DeserializeObject<AuthResponse>(response);

                if (authResponse != null && !string.IsNullOrEmpty(authResponse.access_token))
                {
                    _currentUser = authResponse.user;
                    SupabaseHttpClient.SetAuthToken(authResponse.access_token);
                    SupabasePriceManager._currentUserId = authResponse.user.id;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка авторизации: {ex.Message}");
            }
        }


        public static async Task SignOutAsync()
        {
            try
            {
                _currentUser = null;
                SupabaseHttpClient.SetAuthToken("");
                SupabasePriceManager._currentUserId = "";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выхода: {ex.Message}");
            }
        }

        public static async Task<bool> IsSignedInAsync()
        {
            await Task.CompletedTask;
            return _currentUser != null;
        }

        public static async Task<string> GetCurrentUserEmailAsync()
        {
            await Task.CompletedTask;
            return _currentUser?.email ?? "";
        }

        public static async Task<bool> UpdatePasswordAsync(string newPassword)
        {
            try
            {
                if (_currentUser == null)
                    return false;

                var updateData = new { password = newPassword };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("apikey", SupabaseHttpClient.SUPABASE_ANON_KEY);

                    // Добавляем токен авторизации если есть
                    if (!string.IsNullOrEmpty(SupabaseHttpClient._authToken))
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseHttpClient._authToken}");
                    }

                    var response = await httpClient.PutAsync($"{SupabaseHttpClient.SUPABASE_URL}/auth/v1/user", content);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Ошибка смены пароля: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка смены пароля: {ex.Message}");
            }
        }
    }

    // 5. Обновленный PriceManager с поддержкой Supabase
    public static partial class PriceManager
    {
        private static bool _isOnlineMode = false;

        // Добавляем новый метод инициализации с поддержкой облака
        public static async Task InitializeWithCloudAsync()
        {
            try
            {
                await SupabasePriceManager.InitializeAsync();
                _isOnlineMode = true;
            }
            catch
            {
                _isOnlineMode = false;
                LoadPrices(); // Fallback к локальному режиму
            }
        }

        // Обновленный метод сохранения с поддержкой облака
        public static async Task<bool> SavePricesAsync()
        {
            try
            {
                if (_isOnlineMode)
                {
                    // Проверяем, что _prices инициализирован
                    if (_prices != null)
                    {
                        // Преобразуем текущие цены в формат для Supabase
                        var currentPrices = new Dictionary<string, Dictionary<string, double>>();

                        foreach (var type in _prices.Keys)
                        {
                            currentPrices[type] = new Dictionary<string, double>(_prices[type]);
                        }

                        var success = await SupabasePriceManager.SavePricesToSupabase(currentPrices);

                        if (success)
                        {
                            SavePrices(); // Также сохраняем локально как backup
                            return true;
                        }
                    }
                }
                else
                {
                    SavePrices(); // Локальное сохранение
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения в облако: {ex.Message}\nСохраняем локально.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                SavePrices(); // Fallback к локальному сохранению
                return true;
            }
            return false;
        }

        public static bool IsOnlineMode()
        {
            return _isOnlineMode;
        }

        public static string GetModeString()
        {
            return _isOnlineMode ? "Облачный режим" : "Локальный режим";
        }
    }
}
