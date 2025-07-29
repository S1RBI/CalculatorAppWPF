using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;

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

    // 6. Менеджер для хоккейных цен в Supabase
    public static class SupabaseHockeyPriceManager
    {
        private static Dictionary<string, Dictionary<string, double>> _localHockeyPrices;
        private static HockeyRinkCoefficients _localCoefficients;
        private static int _currentHockeyVersion = 1;
        private static int _currentCoefficientsVersion = 1;

        public static async Task InitializeAsync()
        {
            try
            {
                await LoadHockeyPricesFromSupabase();
                await LoadCoefficientsFromSupabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к облаку для хоккейных цен. Работаем в локальном режиме.\n\nДетали ошибки: {ex.Message}",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Загружаем локальные цены как fallback
                HockeyRinkPriceManager.LoadPrices();
                HockeyRinkPriceManager.LoadCoefficients();
            }
        }

        public static async Task<bool> LoadHockeyPricesFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.hockey_prices&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentHockeyVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localHockeyPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonString);

                        // Обновляем локальный HockeyRinkPriceManager
                        UpdateLocalHockeyPriceManager();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки хоккейных цен из облака: {ex.Message}");
            }
        }

        public static async Task<bool> LoadCoefficientsFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.hockey_coefficients&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentCoefficientsVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localCoefficients = JsonConvert.DeserializeObject<HockeyRinkCoefficients>(jsonString);

                        // Обновляем локальный HockeyRinkPriceManager
                        HockeyRinkPriceManager.UpdateCoefficients(_localCoefficients);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки коэффициентов из облака: {ex.Message}");
            }
        }

        public static async Task<bool> SaveHockeyPricesToSupabase(Dictionary<string, Dictionary<string, double>> hockeyPrices)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentHockeyVersion + 1;

                var appDataForSave = new
                {
                    data_type = "hockey_prices",
                    data_content = hockeyPrices,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.hockey_prices", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentHockeyVersion = newVersion;
                        _localHockeyPrices = hockeyPrices;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentHockeyVersion = newVersion;
                        _localHockeyPrices = hockeyPrices;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения хоккейных цен в облако: {ex.Message}");
            }
        }

        public static async Task<bool> SaveCoefficientsToSupabase(HockeyRinkCoefficients coefficients)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentCoefficientsVersion + 1;

                var appDataForSave = new
                {
                    data_type = "hockey_coefficients",
                    data_content = coefficients,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.hockey_coefficients", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentCoefficientsVersion = newVersion;
                        _localCoefficients = coefficients;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentCoefficientsVersion = newVersion;
                        _localCoefficients = coefficients;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения коэффициентов в облако: {ex.Message}");
            }
        }

        private static void UpdateLocalHockeyPriceManager()
        {
            if (_localHockeyPrices != null)
            {
                var localItems = ConvertHockeyToLocalFormat(_localHockeyPrices);
                HockeyRinkPriceManager.UpdatePrices(localItems);
            }
        }

        private static List<HockeyRinkPriceItem> ConvertHockeyToLocalFormat(Dictionary<string, Dictionary<string, double>> prices)
        {
            var items = new List<HockeyRinkPriceItem>();
            foreach (var category in prices.Keys)
            {
                foreach (var subcategory in prices[category].Keys)
                {
                    items.Add(new HockeyRinkPriceItem
                    {
                        Category = category,
                        Subcategory = subcategory,
                        Name = $"{category} - {subcategory}",
                        Price = prices[category][subcategory]
                    });
                }
            }
            return items;
        }

        public static Dictionary<string, Dictionary<string, double>> GetLocalHockeyPrices()
        {
            return _localHockeyPrices ?? new Dictionary<string, Dictionary<string, double>>();
        }

        public static HockeyRinkCoefficients GetLocalCoefficients()
        {
            return _localCoefficients ?? new HockeyRinkCoefficients();
        }

        public static int GetCurrentHockeyVersion()
        {
            return _currentHockeyVersion;
        }

        public static int GetCurrentCoefficientsVersion()
        {
            return _currentCoefficientsVersion;
        }
    }

    // 7. Менеджер для УСП из круглой трубы в Supabase
    public static class SupabaseUSPRoundPriceManager
    {
        private static Dictionary<string, Dictionary<string, double>> _localUSPRoundPrices;
        private static USPRoundCoefficients _localUSPRoundCoefficients;
        private static USPRoundFixedValues _localUSPRoundFixedValues;
        private static int _currentUSPRoundVersion = 1;
        private static int _currentUSPRoundCoefficientsVersion = 1;
        private static int _currentUSPRoundFixedValuesVersion = 1;

        public static async Task InitializeAsync()
        {
            try
            {
                await LoadUSPRoundPricesFromSupabase();
                await LoadUSPRoundCoefficientsFromSupabase();
                await LoadUSPRoundFixedValuesFromSupabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к облаку для УСП из круглой трубы. Работаем в локальном режиме.\n\nДетали ошибки: {ex.Message}",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                USPRoundPriceManager.LoadPrices();
                USPRoundPriceManager.LoadCoefficients();
                USPRoundPriceManager.LoadFixedValues();
            }
        }

        public static async Task<bool> LoadUSPRoundPricesFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.usp_round_prices&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentUSPRoundVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localUSPRoundPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonString);

                        UpdateLocalUSPRoundPriceManager();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки УСП из круглой трубы цен из облака: {ex.Message}");
            }
        }

        public static async Task<bool> LoadUSPRoundCoefficientsFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.usp_round_coefficients&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentUSPRoundCoefficientsVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localUSPRoundCoefficients = JsonConvert.DeserializeObject<USPRoundCoefficients>(jsonString);

                        USPRoundPriceManager.UpdateCoefficients(_localUSPRoundCoefficients);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки УСП из круглой трубы коэффициентов из облака: {ex.Message}");
            }
        }

        public static async Task<bool> LoadUSPRoundFixedValuesFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.usp_round_fixed_values&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentUSPRoundFixedValuesVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localUSPRoundFixedValues = JsonConvert.DeserializeObject<USPRoundFixedValues>(jsonString);

                        USPRoundPriceManager.UpdateFixedValues(_localUSPRoundFixedValues);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки УСП из круглой трубы фиксированных значений из облака: {ex.Message}");
            }
        }

        public static async Task<bool> SaveUSPRoundPricesToSupabase(Dictionary<string, Dictionary<string, double>> uspRoundPrices)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentUSPRoundVersion + 1;

                var appDataForSave = new
                {
                    data_type = "usp_round_prices",
                    data_content = uspRoundPrices,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.usp_round_prices", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentUSPRoundVersion = newVersion;
                        _localUSPRoundPrices = uspRoundPrices;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentUSPRoundVersion = newVersion;
                        _localUSPRoundPrices = uspRoundPrices;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения УСП из круглой трубы цен в облако: {ex.Message}");
            }
        }

        public static async Task<bool> SaveUSPRoundCoefficientsToSupabase(USPRoundCoefficients coefficients)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentUSPRoundCoefficientsVersion + 1;

                var appDataForSave = new
                {
                    data_type = "usp_round_coefficients",
                    data_content = coefficients,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.usp_round_coefficients", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentUSPRoundCoefficientsVersion = newVersion;
                        _localUSPRoundCoefficients = coefficients;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentUSPRoundCoefficientsVersion = newVersion;
                        _localUSPRoundCoefficients = coefficients;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения УСП из круглой трубы коэффициентов в облако: {ex.Message}");
            }
        }

        public static async Task<bool> SaveUSPRoundFixedValuesToSupabase(USPRoundFixedValues fixedValues)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentUSPRoundFixedValuesVersion + 1;

                var appDataForSave = new
                {
                    data_type = "usp_round_fixed_values",
                    data_content = fixedValues,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.usp_round_fixed_values", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentUSPRoundFixedValuesVersion = newVersion;
                        _localUSPRoundFixedValues = fixedValues;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentUSPRoundFixedValuesVersion = newVersion;
                        _localUSPRoundFixedValues = fixedValues;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения УСП из круглой трубы фиксированных значений в облако: {ex.Message}");
            }
        }

        private static void UpdateLocalUSPRoundPriceManager()
        {
            if (_localUSPRoundPrices != null)
            {
                var localItems = ConvertUSPRoundToLocalFormat(_localUSPRoundPrices);
                USPRoundPriceManager.UpdatePrices(localItems);
            }
        }

        private static List<USPRoundPriceItem> ConvertUSPRoundToLocalFormat(Dictionary<string, Dictionary<string, double>> prices)
        {
            var items = new List<USPRoundPriceItem>();
            foreach (var category in prices.Keys)
            {
                foreach (var subcategory in prices[category].Keys)
                {
                    string unit = "м2";
                    if (category.Contains("Комплект ворот") || category.Contains("Дополнительная"))
                        unit = category.Contains("стойка") ? "шт" : "компл.";

                    items.Add(new USPRoundPriceItem
                    {
                        Category = category,
                        Subcategory = subcategory,
                        Name = $"{category} - {subcategory}",
                        Price = prices[category][subcategory],
                        Unit = unit
                    });
                }
            }
            return items;
        }

        public static Dictionary<string, Dictionary<string, double>> GetLocalUSPRoundPrices()
        {
            return _localUSPRoundPrices ?? new Dictionary<string, Dictionary<string, double>>();
        }

        public static USPRoundCoefficients GetLocalUSPRoundCoefficients()
        {
            return _localUSPRoundCoefficients ?? new USPRoundCoefficients();
        }

        public static USPRoundFixedValues GetLocalUSPRoundFixedValues()
        {
            return _localUSPRoundFixedValues ?? new USPRoundFixedValues();
        }

        public static int GetCurrentUSPRoundVersion()
        {
            return _currentUSPRoundVersion;
        }

        public static int GetCurrentUSPRoundCoefficientsVersion()
        {
            return _currentUSPRoundCoefficientsVersion;
        }

        public static int GetCurrentUSPRoundFixedValuesVersion()
        {
            return _currentUSPRoundFixedValuesVersion;
        }
    }

    // 8. Менеджер для УСП цен в Supabase
    public static class SupabaseUSPPriceManager
    {
        private static Dictionary<string, Dictionary<string, double>> _localUSPPrices;
        private static USPCoefficients _localUSPCoefficients;
        private static int _currentUSPVersion = 1;
        private static int _currentUSPCoefficientsVersion = 1;

        public static async Task InitializeAsync()
        {
            try
            {
                await LoadUSPPricesFromSupabase();
                await LoadUSPCoefficientsFromSupabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к облаку для УСП цен. Работаем в локальном режиме.\n\nДетали ошибки: {ex.Message}",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Загружаем локальные цены как fallback
                USPPriceManager.LoadPrices();
                USPPriceManager.LoadCoefficients();
            }
        }

        public static async Task<bool> LoadUSPPricesFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.usp_prices&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentUSPVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localUSPPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonString);

                        // Обновляем локальный USPPriceManager
                        UpdateLocalUSPPriceManager();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки УСП цен из облака: {ex.Message}");
            }
        }

        public static async Task<bool> LoadUSPCoefficientsFromSupabase()
        {
            try
            {
                var response = await SupabaseHttpClient.GetAsync("app_data?data_type=eq.usp_coefficients&order=version.desc&limit=1");

                if (!string.IsNullOrEmpty(response) && response.StartsWith("[") && response.EndsWith("]"))
                {
                    var appDataList = JsonConvert.DeserializeObject<List<AppData>>(response);

                    if (appDataList != null && appDataList.Count > 0)
                    {
                        var appData = appDataList[0];
                        _currentUSPCoefficientsVersion = appData.version;

                        var jsonString = JsonConvert.SerializeObject(appData.data_content);
                        _localUSPCoefficients = JsonConvert.DeserializeObject<USPCoefficients>(jsonString);

                        // Обновляем локальный USPPriceManager
                        USPPriceManager.UpdateCoefficients(_localUSPCoefficients);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки УСП коэффициентов из облака: {ex.Message}");
            }
        }

        public static async Task<bool> SaveUSPPricesToSupabase(Dictionary<string, Dictionary<string, double>> uspPrices)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentUSPVersion + 1;

                var appDataForSave = new
                {
                    data_type = "usp_prices",
                    data_content = uspPrices,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.usp_prices", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentUSPVersion = newVersion;
                        _localUSPPrices = uspPrices;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentUSPVersion = newVersion;
                        _localUSPPrices = uspPrices;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения УСП цен в облако: {ex.Message}");
            }
        }

        public static async Task<bool> SaveUSPCoefficientsToSupabase(USPCoefficients coefficients)
        {
            try
            {
                if (!await SupabasePriceManager.IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentUSPCoefficientsVersion + 1;

                var appDataForSave = new
                {
                    data_type = "usp_coefficients",
                    data_content = coefficients,
                    version = newVersion,
                    created_by = string.IsNullOrEmpty(SupabasePriceManager._currentUserId) ? null : SupabasePriceManager._currentUserId
                };

                var json = JsonConvert.SerializeObject(appDataForSave);

                try
                {
                    var updateResponse = await SupabaseHttpClient.PutAsync("app_data?data_type=eq.usp_coefficients", json);
                    if (!string.IsNullOrEmpty(updateResponse))
                    {
                        _currentUSPCoefficientsVersion = newVersion;
                        _localUSPCoefficients = coefficients;
                        return true;
                    }
                }
                catch
                {
                    var response = await SupabaseHttpClient.PostAsync("app_data", json);
                    if (!string.IsNullOrEmpty(response))
                    {
                        _currentUSPCoefficientsVersion = newVersion;
                        _localUSPCoefficients = coefficients;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения УСП коэффициентов в облако: {ex.Message}");
            }
        }

        private static void UpdateLocalUSPPriceManager()
        {
            if (_localUSPPrices != null)
            {
                var localItems = ConvertUSPToLocalFormat(_localUSPPrices);
                USPPriceManager.UpdatePrices(localItems);
            }
        }

        private static List<USPPriceItem> ConvertUSPToLocalFormat(Dictionary<string, Dictionary<string, double>> prices)
        {
            var items = new List<USPPriceItem>();
            foreach (var category in prices.Keys)
            {
                foreach (var subcategory in prices[category].Keys)
                {
                    // Определяем единицу измерения
                    string unit = category.Contains("ворота") ? "компл." : "м2";

                    items.Add(new USPPriceItem
                    {
                        Category = category,
                        Subcategory = subcategory,
                        Name = $"{category} - {subcategory}",
                        Price = prices[category][subcategory],
                        Unit = unit
                    });
                }
            }
            return items;
        }

        public static Dictionary<string, Dictionary<string, double>> GetLocalUSPPrices()
        {
            return _localUSPPrices ?? new Dictionary<string, Dictionary<string, double>>();
        }

        public static USPCoefficients GetLocalUSPCoefficients()
        {
            return _localUSPCoefficients ?? new USPCoefficients();
        }

        public static int GetCurrentUSPVersion()
        {
            return _currentUSPVersion;
        }

        public static int GetCurrentUSPCoefficientsVersion()
        {
            return _currentUSPCoefficientsVersion;
        }
    }
}
