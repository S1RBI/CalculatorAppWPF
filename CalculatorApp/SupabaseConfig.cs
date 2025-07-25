using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;
using System.Windows;

namespace CalculatorApp
{
    // 1. Конфигурация Supabase
    public static class SupabaseConfig
    {
        // ЗАМЕНИТЕ НА ВАШИ ДАННЫЕ ИЗ SUPABASE DASHBOARD
        public const string SUPABASE_URL = "https://krszxihgewwrawskrpco.supabase.co";
        public const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imtyc3p4aWhnZXd3cmF3c2tycGNvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTMyNjYyNDYsImV4cCI6MjA2ODg0MjI0Nn0.yJYHLORsTPuS0vHKPZEcOQ6g6bfXpYBhZKa7jtN17zg";

        private static Supabase.Client _client;

        public static async Task<Supabase.Client> GetClient()
        {
            if (_client == null)
            {
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = false,
                    AutoRefreshToken = true
                };

                _client = new Supabase.Client(SUPABASE_URL, SUPABASE_ANON_KEY, options);
                await _client.InitializeAsync();
            }

            return _client;
        }
    }

    // 2. Модели данных для Supabase
    [Table("app_data")]
    public class AppData : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; }

        [Column("data_type")]
        public string DataType { get; set; }

        [Column("data_content")]
        public Dictionary<string, object> DataContent { get; set; }

        [Column("version")]
        public int Version { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; }
    }

    [Table("user_profiles")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
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
                MessageBox.Show($"Ошибка подключения к облаку. Работаем в локальном режиме.\n{ex.Message}",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Загружаем локальные цены как fallback
                PriceManager.LoadPrices();
            }
        }

        public static async Task<bool> LoadPricesFromSupabase()
        {
            try
            {
                var client = await SupabaseConfig.GetClient();

                var response = await client
                    .From<AppData>()
                    .Select("*")
                    .Where(x => x.DataType == "prices")
                    .Order("version", Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                if (response.Models.Count > 0)
                {
                    var appData = response.Models[0];
                    _currentVersion = appData.Version;

                    // Преобразуем JSON обратно в нужный формат
                    var jsonString = JsonConvert.SerializeObject(appData.DataContent);
                    _localPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonString);

                    // Обновляем локальный PriceManager
                    UpdateLocalPriceManager();

                    return true;
                }

                return false;
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
                var client = await SupabaseConfig.GetClient();

                // Проверяем, есть ли права администратора
                if (!await IsCurrentUserAdmin())
                {
                    throw new Exception("Недостаточно прав для сохранения данных");
                }

                var newVersion = _currentVersion + 1;

                var appData = new AppData
                {
                    DataType = "prices",
                    DataContent = ConvertPricesToDictionary(prices),
                    Version = newVersion,
                    CreatedBy = client.Auth.CurrentUser?.Id
                };

                var response = await client
                    .From<AppData>()
                    .Insert(appData);

                if (response.Models.Count > 0)
                {
                    _currentVersion = newVersion;
                    _localPrices = prices;

                    // Также сохраняем локально как backup
                    PriceManager.UpdatePrices(ConvertToLocalFormat(prices));
                    PriceManager.SavePrices();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения цен в облако: {ex.Message}");
            }
        }

        private static Dictionary<string, object> ConvertPricesToDictionary(Dictionary<string, Dictionary<string, double>> prices)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in prices)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
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

        public static async Task<bool> IsCurrentUserAdmin()
        {
            try
            {
                var client = await SupabaseConfig.GetClient();

                if (client.Auth.CurrentUser == null)
                    return false;

                var response = await client
                    .From<UserProfile>()
                    .Select("is_admin")
                    .Where(x => x.Id == client.Auth.CurrentUser.Id)
                    .Single();

                return response?.IsAdmin ?? false;
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

    // 4. Менеджер аутентификации Supabase
    public static class SupabaseAuthManager
    {
        public static async Task<bool> SignInAsync(string email, string password)
        {
            try
            {
                var client = await SupabaseConfig.GetClient();
                var session = await client.Auth.SignIn(email, password);
                return session != null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка авторизации: {ex.Message}");
            }
        }

        public static async Task<bool> SignUpAsync(string email, string password)
        {
            try
            {
                var client = await SupabaseConfig.GetClient();
                var session = await client.Auth.SignUp(email, password);
                return session != null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка регистрации: {ex.Message}");
            }
        }

        public static async Task SignOutAsync()
        {
            try
            {
                var client = await SupabaseConfig.GetClient();
                await client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выхода: {ex.Message}");
            }
        }

        public static async Task<bool> IsSignedInAsync()
        {
            try
            {
                var client = await SupabaseConfig.GetClient();
                return client.Auth.CurrentUser != null;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> GetCurrentUserEmailAsync()
        {
            try
            {
                var client = await SupabaseConfig.GetClient();
                return client.Auth.CurrentUser?.Email ?? "";
            }
            catch
            {
                return "";
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

