// Вставьте сюда код USPPriceManager.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CalculatorApp
{
    // Менеджер цен для УСП
    public static class USPPriceManager
    {
        private static readonly string USPDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiKom Calculator"
        );

        private static readonly string USPPricesFile = Path.Combine(USPDataFolder, "usp_prices.json");
        private static readonly string USPCoefficientsFile = Path.Combine(USPDataFolder, "usp_coefficients.json");

        private static Dictionary<string, Dictionary<string, double>> _uspPrices;
        private static USPCoefficients _coefficients;
        private static bool _isOnlineMode = false;

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(USPDataFolder))
                {
                    Directory.CreateDirectory(USPDataFolder);
                }

                LoadPrices();
                LoadCoefficients();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации цен УСП: {ex.Message}");
                InitializeDefaultPrices();
                InitializeDefaultCoefficients();
            }
        }

        // Новый метод для инициализации с поддержкой облака
        public static async Task InitializeWithCloudAsync()
        {
            try
            {
                await SupabaseUSPPriceManager.InitializeAsync();

                // Попытка загрузить данные из облака
                try
                {
                    // Предполагаем, что методы Supabase должны возвращать данные, а не bool
                    // Если они действительно возвращают bool, то нужно создать другие методы

                    // Временно комментируем проблемные вызовы и используем локальные данные
                    System.Diagnostics.Debug.WriteLine("УСП: Инициализация Supabase завершена, используем локальные данные как fallback");
                    LoadPrices();
                    LoadCoefficients();

                    // TODO: Исправить методы SupabaseUSPPriceManager для возврата правильных типов данных
                    // var cloudPrices = await SupabaseUSPPriceManager.GetUSPPricesAsync();
                    // var cloudCoefficients = await SupabaseUSPPriceManager.GetUSPCoefficientsAsync();

                    System.Diagnostics.Debug.WriteLine($"УСП: Загружены локальные данные: {_uspPrices?.Values?.Sum(v => v.Count) ?? 0} позиций");
                    System.Diagnostics.Debug.WriteLine($"УСП: Коэффициенты: оптовый={_coefficients?.WholesaleCoeff ?? 0}, дилерский={_coefficients?.DealerCoeff ?? 0}");
                }
                catch (Exception cloudEx)
                {
                    System.Diagnostics.Debug.WriteLine($"УСП: Ошибка работы с облачными данными: {cloudEx.Message}");
                    LoadPrices(); // Fallback к локальным данным
                    LoadCoefficients();
                }

                _isOnlineMode = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"УСП: переход в локальный режим - {ex.Message}");
                _isOnlineMode = false;
                Initialize(); // Fallback к локальному режиму
                throw;
            }
        }

        public static void LoadPrices()
        {
            try
            {
                if (File.Exists(USPPricesFile))
                {
                    var json = File.ReadAllText(USPPricesFile);
                    _uspPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(json);
                    System.Diagnostics.Debug.WriteLine($"УСП: Загружено {_uspPrices?.Values?.Sum(v => v.Count) ?? 0} позиций из локального файла");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("УСП: Локальный файл не найден, инициализируем значения по умолчанию");
                    InitializeDefaultPrices();
                    SavePrices();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"УСП: Ошибка загрузки локальных данных: {ex.Message}");
                InitializeDefaultPrices();
            }
        }

        public static void LoadCoefficients()
        {
            try
            {
                if (File.Exists(USPCoefficientsFile))
                {
                    var json = File.ReadAllText(USPCoefficientsFile);
                    _coefficients = JsonConvert.DeserializeObject<USPCoefficients>(json);
                }
                else
                {
                    InitializeDefaultCoefficients();
                    SaveCoefficients();
                }
            }
            catch
            {
                InitializeDefaultCoefficients();
            }
        }

        private static void InitializeDefaultPrices()
        {
            _uspPrices = new Dictionary<string, Dictionary<string, double>>
            {
                ["УСП H=3м без встроенных ворот для мини футбола и баскетбольных щитов"] =
                    new Dictionary<string, double> { ["Столбы 80х80"] = 3349, ["Столбы 100х100"] = 3696 },
                ["УСП Н=3 м. Ворота для мини футбола с баскетбольным щитом"] =
                    new Dictionary<string, double> { ["Столбы 80х80"] = 235699, ["Столбы 100х100"] = 248695 },
                ["УСП H=4м без встроенных ворот для мини футбола и баскетбольных щитов"] =
                    new Dictionary<string, double> { ["Столбы 80х80"] = 3051, ["Столбы 100х100"] = 3341 },
                ["УСП Н=4 м. Ворота для мини футбола с баскетбольным щитом"] =
                    new Dictionary<string, double> { ["Столбы 80х80"] = 245560, ["Столбы 100х100"] = 259498 }
            };
        }

        private static void InitializeDefaultCoefficients()
        {
            _coefficients = new USPCoefficients
            {
                WholesaleCoeff = 1.8,  // Коэффициент оптовый
                DealerCoeff = 1.3      // Коэффициент дилерский
            };
        }

        public static void SavePrices()
        {
            try
            {
                if (!Directory.Exists(USPDataFolder))
                {
                    Directory.CreateDirectory(USPDataFolder);
                }

                var json = JsonConvert.SerializeObject(_uspPrices, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(USPPricesFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения цен УСП: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveCoefficients()
        {
            try
            {
                if (!Directory.Exists(USPDataFolder))
                {
                    Directory.CreateDirectory(USPDataFolder);
                }

                var json = JsonConvert.SerializeObject(_coefficients, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(USPCoefficientsFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения коэффициентов УСП: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static double GetUSPPrice(string height, string columnType, bool withGates)
        {
            string category;

            if (withGates)
            {
                if (height == "3м")
                    category = "УСП Н=3 м. Ворота для мини футбола с баскетбольным щитом";
                else
                    category = "УСП Н=4 м. Ворота для мини футбола с баскетбольным щитом";
            }
            else
            {
                category = $"УСП H={height} без встроенных ворот для мини футбола и баскетбольных щитов";
            }

            var key = $"Столбы {columnType}";

            System.Diagnostics.Debug.WriteLine($"УСП: Поиск цены для категории '{category}', ключ '{key}'");
            System.Diagnostics.Debug.WriteLine($"УСП: _uspPrices не null: {_uspPrices != null}");

            if (_uspPrices != null)
            {
                System.Diagnostics.Debug.WriteLine($"УСП: Доступные категории: {string.Join(", ", _uspPrices.Keys)}");

                if (_uspPrices.ContainsKey(category))
                {
                    System.Diagnostics.Debug.WriteLine($"УСП: Найдена категория, доступные ключи: {string.Join(", ", _uspPrices[category].Keys)}");

                    if (_uspPrices[category].ContainsKey(key))
                    {
                        var price = _uspPrices[category][key];
                        System.Diagnostics.Debug.WriteLine($"УСП: Найдена цена: {price}");
                        return price;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"УСП: Ключ '{key}' не найден в категории");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"УСП: Категория '{category}' не найдена");
                }
            }

            // Возвращаем значения по умолчанию
            double defaultPrice;
            if (withGates)
            {
                if (height == "3м")
                    defaultPrice = columnType == "80х80" ? 235699 : 248695;
                else
                    defaultPrice = columnType == "80х80" ? 245560 : 259498;
            }
            else
            {
                if (height == "3м")
                    defaultPrice = columnType == "80х80" ? 3349 : 3696;
                else
                    defaultPrice = columnType == "80х80" ? 3051 : 3341;
            }

            System.Diagnostics.Debug.WriteLine($"УСП: Используется цена по умолчанию: {defaultPrice}");
            return defaultPrice;
        }

        public static USPCoefficients GetCoefficients()
        {
            return _coefficients ?? new USPCoefficients();
        }

        public static void UpdateCoefficients(USPCoefficients newCoefficients)
        {
            _coefficients = newCoefficients;
            SaveCoefficients();
        }

        public static List<USPPriceItem> GetAllPrices()
        {
            var items = new List<USPPriceItem>();

            if (_uspPrices != null)
            {
                foreach (var category in _uspPrices.Keys)
                {
                    foreach (var subcategory in _uspPrices[category].Keys)
                    {
                        // Определяем единицу измерения
                        string unit = category.Contains("ворота") ? "компл." : "м2";

                        items.Add(new USPPriceItem
                        {
                            Category = category,
                            Subcategory = subcategory,
                            Name = $"{category} - {subcategory}",
                            Price = _uspPrices[category][subcategory],
                            Unit = unit
                        });
                    }
                }
            }

            return items;
        }

        public static void UpdatePrices(List<USPPriceItem> items)
        {
            if (_uspPrices == null)
            {
                _uspPrices = new Dictionary<string, Dictionary<string, double>>();
            }

            _uspPrices.Clear();

            foreach (var item in items)
            {
                if (!_uspPrices.ContainsKey(item.Category))
                {
                    _uspPrices[item.Category] = new Dictionary<string, double>();
                }

                _uspPrices[item.Category][item.Subcategory] = item.Price;
            }

            SavePrices();
        }

        // Новый асинхронный метод сохранения с поддержкой облака
        public static async Task<bool> SavePricesAsync(List<USPPriceItem> items, USPCoefficients coefficients)
        {
            try
            {
                if (_isOnlineMode)
                {
                    // Обновляем локальные данные
                    UpdatePrices(items);
                    UpdateCoefficients(coefficients);

                    // Преобразуем в формат для Supabase
                    var uspPricesForSupabase = new Dictionary<string, Dictionary<string, double>>();
                    foreach (var item in items)
                    {
                        if (!uspPricesForSupabase.ContainsKey(item.Category))
                        {
                            uspPricesForSupabase[item.Category] = new Dictionary<string, double>();
                        }
                        uspPricesForSupabase[item.Category][item.Subcategory] = item.Price;
                    }

                    // Сохраняем цены и коэффициенты в облако
                    var pricesSuccess = await SupabaseUSPPriceManager.SaveUSPPricesToSupabase(uspPricesForSupabase);
                    var coefficientsSuccess = await SupabaseUSPPriceManager.SaveUSPCoefficientsToSupabase(coefficients);

                    if (pricesSuccess && coefficientsSuccess)
                    {
                        // Также сохраняем локально как backup
                        SavePrices();
                        SaveCoefficients();
                        return true;
                    }
                }
                else
                {
                    // Локальное сохранение
                    UpdatePrices(items);
                    UpdateCoefficients(coefficients);
                    SavePrices();
                    SaveCoefficients();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения в облако: {ex.Message}\nСохраняем локально.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Fallback к локальному сохранению
                UpdatePrices(items);
                UpdateCoefficients(coefficients);
                SavePrices();
                SaveCoefficients();
                return true;
            }
            return false;
        }

        public static void ResetToDefaults()
        {
            InitializeDefaultPrices();
            InitializeDefaultCoefficients();
            SavePrices();
            SaveCoefficients();
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
