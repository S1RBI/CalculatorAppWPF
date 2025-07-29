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
    // Менеджер цен для хоккейных коробок
    public static class HockeyRinkPriceManager
    {
        private static readonly string HockeyDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiKom Calculator"
        );

        private static readonly string HockeyPricesFile = Path.Combine(HockeyDataFolder, "hockey_prices.json");
        private static readonly string HockeyCoefficientsFile = Path.Combine(HockeyDataFolder, "hockey_coefficients.json");

        private static Dictionary<string, Dictionary<string, double>> _hockeyPrices;
        private static HockeyRinkCoefficients _coefficients;
        private static bool _isOnlineMode = false;

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(HockeyDataFolder))
                {
                    Directory.CreateDirectory(HockeyDataFolder);
                }

                LoadPrices();
                LoadCoefficients();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации цен хоккейных коробок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeDefaultPrices();
                InitializeDefaultCoefficients();
            }
        }

        // Новый метод для инициализации с поддержкой облака
        public static async Task InitializeWithCloudAsync()
        {
            try
            {
                await SupabaseHockeyPriceManager.InitializeAsync();
                _isOnlineMode = true;
            }
            catch
            {
                _isOnlineMode = false;
                Initialize(); // Fallback к локальному режиму
                throw;
            }
        }

        public static void LoadPrices()
        {
            try
            {
                if (File.Exists(HockeyPricesFile))
                {
                    var json = File.ReadAllText(HockeyPricesFile);
                    _hockeyPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(json);
                }
                else
                {
                    InitializeDefaultPrices();
                    SavePrices();
                }
            }
            catch
            {
                InitializeDefaultPrices();
            }
        }

        public static void LoadCoefficients()
        {
            try
            {
                if (File.Exists(HockeyCoefficientsFile))
                {
                    var json = File.ReadAllText(HockeyCoefficientsFile);
                    _coefficients = JsonConvert.DeserializeObject<HockeyRinkCoefficients>(json);
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
            _hockeyPrices = new Dictionary<string, Dictionary<string, double>>
            {
                ["Стеклопластик"] = new Dictionary<string, double> { ["5мм"] = 15500, ["7мм"] = 16500 },
                ["Защитная сетка в бросковой зоне"] = new Dictionary<string, double> { ["1,5м"] = 5250, ["2м"] = 6950 },
                ["Защитная сетка по периметру"] = new Dictionary<string, double> { ["1,5м"] = 4500, ["2м"] = 6200 },
                ["Защитная сетка при заказе отедльно"] = new Dictionary<string, double> { ["1,5м"] = 6000, ["2м"] = 7600 },
                ["Дополнительные элементы"] = new Dictionary<string, double> { ["Калитка"] = 33000, ["Тех. ворота"] = 85000 }
            };
        }

        private static void InitializeDefaultCoefficients()
        {
            _coefficients = new HockeyRinkCoefficients
            {
                DealerCoeff = 1.25,
                WholesaleCoeff = 1.5,  // Согласно JSON
                EstimateCoeff = 1.8    // Согласно JSON
            };
        }

        public static void SavePrices()
        {
            try
            {
                if (!Directory.Exists(HockeyDataFolder))
                {
                    Directory.CreateDirectory(HockeyDataFolder);
                }

                var json = JsonConvert.SerializeObject(_hockeyPrices, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(HockeyPricesFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения цен хоккейных коробок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveCoefficients()
        {
            try
            {
                if (!Directory.Exists(HockeyDataFolder))
                {
                    Directory.CreateDirectory(HockeyDataFolder);
                }

                var json = JsonConvert.SerializeObject(_coefficients, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(HockeyCoefficientsFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения коэффициентов хоккейных коробок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static double GetGlassPrice(string thickness)
        {
            var category = "Стеклопластик";

            if (_hockeyPrices != null && _hockeyPrices.ContainsKey(category) &&
                _hockeyPrices[category].ContainsKey(thickness))
            {
                return _hockeyPrices[category][thickness];
            }
            return thickness == "5мм" ? 15500 : 16500; // default values
        }

        public static double GetNetPrice(string category, string height)
        {
            if (_hockeyPrices != null && _hockeyPrices.ContainsKey(category) &&
                _hockeyPrices[category].ContainsKey(height))
            {
                return _hockeyPrices[category][height];
            }

            // Возвращаем значения по умолчанию
            if (category == "Защитная сетка в бросковой зоне")
                return height == "1,5м" ? 5250 : 6950;
            else if (category == "Защитная сетка по периметру")
                return height == "1,5м" ? 4500 : 6200;
            else if (category == "Защитная сетка при заказе отдельно" || category == "Защитная сетка при заказе отедльно")
                return height == "1,5м" ? 6000 : 7600;

            return 0;
        }

        public static double GetGatePrice()
        {
            if (_hockeyPrices != null && _hockeyPrices.ContainsKey("Дополнительные элементы") &&
                _hockeyPrices["Дополнительные элементы"].ContainsKey("Калитка"))
            {
                return _hockeyPrices["Дополнительные элементы"]["Калитка"];
            }
            return 33000; // default
        }

        public static double GetTechGatePrice()
        {
            if (_hockeyPrices != null && _hockeyPrices.ContainsKey("Дополнительные элементы") &&
                _hockeyPrices["Дополнительные элементы"].ContainsKey("Тех. ворота"))
            {
                return _hockeyPrices["Дополнительные элементы"]["Тех. ворота"];
            }
            return 85000; // default как указано в формулах
        }

        public static HockeyRinkCoefficients GetCoefficients()
        {
            return _coefficients ?? new HockeyRinkCoefficients();
        }

        public static void UpdateCoefficients(HockeyRinkCoefficients newCoefficients)
        {
            _coefficients = newCoefficients;
            SaveCoefficients();
        }

        public static List<HockeyRinkPriceItem> GetAllPrices()
        {
            var items = new List<HockeyRinkPriceItem>();

            if (_hockeyPrices != null)
            {
                foreach (var category in _hockeyPrices.Keys)
                {
                    foreach (var subcategory in _hockeyPrices[category].Keys)
                    {
                        items.Add(new HockeyRinkPriceItem
                        {
                            Category = category,
                            Subcategory = subcategory,
                            Name = $"{category} - {subcategory}",
                            Price = _hockeyPrices[category][subcategory]
                        });
                    }
                }
            }

            return items;
        }

        public static void UpdatePrices(List<HockeyRinkPriceItem> items)
        {
            if (_hockeyPrices == null)
            {
                _hockeyPrices = new Dictionary<string, Dictionary<string, double>>();
            }

            _hockeyPrices.Clear();

            foreach (var item in items)
            {
                if (!_hockeyPrices.ContainsKey(item.Category))
                {
                    _hockeyPrices[item.Category] = new Dictionary<string, double>();
                }

                _hockeyPrices[item.Category][item.Subcategory] = item.Price;
            }

            SavePrices();
        }

        // Новый асинхронный метод сохранения с поддержкой облака
        public static async Task<bool> SavePricesAsync(List<HockeyRinkPriceItem> items, HockeyRinkCoefficients coefficients)
        {
            try
            {
                if (_isOnlineMode)
                {
                    // Обновляем локальные данные
                    UpdatePrices(items);
                    UpdateCoefficients(coefficients);

                    // Преобразуем в формат для Supabase
                    var hockeyPricesForSupabase = new Dictionary<string, Dictionary<string, double>>();
                    foreach (var item in items)
                    {
                        if (!hockeyPricesForSupabase.ContainsKey(item.Category))
                        {
                            hockeyPricesForSupabase[item.Category] = new Dictionary<string, double>();
                        }
                        hockeyPricesForSupabase[item.Category][item.Subcategory] = item.Price;
                    }

                    // Сохраняем цены и коэффициенты в облако
                    var pricesSuccess = await SupabaseHockeyPriceManager.SaveHockeyPricesToSupabase(hockeyPricesForSupabase);
                    var coefficientsSuccess = await SupabaseHockeyPriceManager.SaveCoefficientsToSupabase(coefficients);

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
