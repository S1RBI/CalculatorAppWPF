// Вставьте сюда код USPRoundPriceManager.cs
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

    // Менеджер цен для УСП из круглой трубы

    public static class USPRoundPriceManager

    {

        private static readonly string USPRoundDataFolder = Path.Combine(

        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),

        "DiKom Calculator"

        );

        private static readonly string USPRoundPricesFile = Path.Combine(USPRoundDataFolder, "usp_round_prices.json");

        private static readonly string USPRoundCoefficientsFile = Path.Combine(USPRoundDataFolder, "usp_round_coefficients.json");

        private static readonly string USPRoundFixedValuesFile = Path.Combine(USPRoundDataFolder, "usp_round_fixed_values.json");

        private static Dictionary<string, Dictionary<string, double>> _uspRoundPrices;

        private static USPRoundCoefficients _coefficients;

        private static USPRoundFixedValues _fixedValues;

        private static bool _isOnlineMode = false;

        public static void Initialize()

        {

            try

            {

                if (!Directory.Exists(USPRoundDataFolder))

                {

                    Directory.CreateDirectory(USPRoundDataFolder);

                }

                LoadPrices();

                LoadCoefficients();

                LoadFixedValues();

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации цен УСП из круглой трубы: {ex.Message}");

                InitializeDefaultPrices();

                InitializeDefaultCoefficients();

                InitializeDefaultFixedValues();

            }

        }

        public static async Task InitializeWithCloudAsync()

        {

            try

            {

                await SupabaseUSPRoundPriceManager.InitializeAsync();

                _isOnlineMode = true;

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"УСП круглая труба: переход в локальный режим - {ex.Message}");

                _isOnlineMode = false;

                Initialize();
                throw;
            }

        }

        public static void LoadPrices()

        {

            try

            {

                if (File.Exists(USPRoundPricesFile))

                {

                    var json = File.ReadAllText(USPRoundPricesFile);

                    _uspRoundPrices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(json);

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

                if (File.Exists(USPRoundCoefficientsFile))

                {

                    var json = File.ReadAllText(USPRoundCoefficientsFile);

                    _coefficients = JsonConvert.DeserializeObject<USPRoundCoefficients>(json);

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

        public static void LoadFixedValues()

        {

            try

            {

                if (File.Exists(USPRoundFixedValuesFile))

                {

                    var json = File.ReadAllText(USPRoundFixedValuesFile);

                    _fixedValues = JsonConvert.DeserializeObject<USPRoundFixedValues>(json);

                }

                else

                {

                    InitializeDefaultFixedValues();

                    SaveFixedValues();

                }

            }

            catch

            {

                InitializeDefaultFixedValues();

            }

        }

        private static void InitializeDefaultPrices()

        {

            _uspRoundPrices = new Dictionary<string, Dictionary<string, double>>

            {

                ["УСП H=3м без встроенных ворот для мини футбола и баскетбольных щитов"] =

            new Dictionary<string, double> { ["Столбы Ф108"] = 4868.8096 },

                ["УСП H=4,1м без встроенных ворот для мини футбола и баскетбольных щитов"] =

            new Dictionary<string, double> { ["Столбы Ф108"] = 4880.1337 },

                ["Комплект ворот 4,1 м с баскетболкой"] =

            new Dictionary<string, double> { ["Столбы Ф108"] = 432427.4600 },

                ["Дополнительная калитка для высоты УСП 3м"] =

            new Dictionary<string, double> { ["шт"] = 33749.2400 },

                ["Дополнительная калитка для высоты УСП 4,1м"] =

            new Dictionary<string, double> { ["шт"] = 43182.1000 },

                ["Дополнительная баскетбольная стойка"] =

            new Dictionary<string, double> { ["шт"] = 84000.0000 }

            };

        }

        private static void InitializeDefaultCoefficients()

        {

            _coefficients = new USPRoundCoefficients

            {

                WholesaleCoeff = 1.202,

                DealerCoeff = 1.149,

                PowderPricePerM2 = 150,

                PaintingCoeff = 1.3,

                PaintingSecondCoeff = 2.3

            };

        }

        private static void InitializeDefaultFixedValues()

        {

            _fixedValues = new USPRoundFixedValues

            {

                BasketballStand = new USPRoundFixedValues.ElementValues { Mass = 130, Volume = 1.5 },

                Gate3m = new USPRoundFixedValues.ElementValues { Mass = 81, Volume = 0.2 },

                Gate41m = new USPRoundFixedValues.ElementValues { Mass = 95, Volume = 0.25 }

            };

        }

        public static void SavePrices()

        {

            try

            {

                if (!Directory.Exists(USPRoundDataFolder))

                {

                    Directory.CreateDirectory(USPRoundDataFolder);

                }

                var json = JsonConvert.SerializeObject(_uspRoundPrices, Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText(USPRoundPricesFile, json);

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Ошибка сохранения цен УСП из круглой трубы: {ex.Message}", "Ошибка",

                MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }

        public static void SaveCoefficients()

        {

            try

            {

                if (!Directory.Exists(USPRoundDataFolder))

                {

                    Directory.CreateDirectory(USPRoundDataFolder);

                }

                var json = JsonConvert.SerializeObject(_coefficients, Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText(USPRoundCoefficientsFile, json);

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Ошибка сохранения коэффициентов УСП из круглой трубы: {ex.Message}", "Ошибка",

                MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }

        public static void SaveFixedValues()

        {

            try

            {

                if (!Directory.Exists(USPRoundDataFolder))

                {

                    Directory.CreateDirectory(USPRoundDataFolder);

                }

                var json = JsonConvert.SerializeObject(_fixedValues, Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText(USPRoundFixedValuesFile, json);

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Ошибка сохранения фиксированных значений УСП из круглой трубы: {ex.Message}", "Ошибка",

                MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }

        public static double GetPrice(string category, string subcategory)

        {

            if (_uspRoundPrices != null && _uspRoundPrices.ContainsKey(category) &&

            _uspRoundPrices[category].ContainsKey(subcategory))

            {

                return _uspRoundPrices[category][subcategory];

            }

            // Возвращаем значения по умолчанию

            if (category == "УСП H=3м без встроенных ворот для мини футбола и баскетбольных щитов")

                return 4868.8096;

            else if (category == "УСП H=4,1м без встроенных ворот для мини футбола и баскетбольных щитов")

                return 4880.1337;

            else if (category == "Комплект ворот 4,1 м с баскетболкой")

                return 432427.4600;

            else if (category == "Дополнительная калитка для высоты УСП 3м")

                return 33749.2400;

            else if (category == "Дополнительная калитка для высоты УСП 4,1м")

                return 43182.1000;

            else if (category == "Дополнительная баскетбольная стойка")

                return 84000.0000;

            return 0;

        }

        public static USPRoundCoefficients GetCoefficients()

        {

            return _coefficients ?? new USPRoundCoefficients();

        }

        public static USPRoundFixedValues GetFixedValues()

        {

            return _fixedValues ?? new USPRoundFixedValues();

        }

        public static void UpdateCoefficients(USPRoundCoefficients newCoefficients)

        {

            _coefficients = newCoefficients;

            SaveCoefficients();

        }

        public static void UpdateFixedValues(USPRoundFixedValues newFixedValues)

        {

            _fixedValues = newFixedValues;

            SaveFixedValues();

        }

        public static List<USPRoundPriceItem> GetAllPrices()

        {

            var items = new List<USPRoundPriceItem>();

            if (_uspRoundPrices != null)

            {

                foreach (var category in _uspRoundPrices.Keys)

                {

                    foreach (var subcategory in _uspRoundPrices[category].Keys)

                    {

                        string unit = "м2";

                        if (category.Contains("Комплект ворот") || category.Contains("Дополнительная"))

                            unit = category.Contains("стойка") ? "шт" : "компл.";

                        items.Add(new USPRoundPriceItem

                        {

                            Category = category,

                            Subcategory = subcategory,

                            Name = $"{category} - {subcategory}",

                            Price = _uspRoundPrices[category][subcategory],

                            Unit = unit

                        });

                    }

                }

            }

            return items;

        }

        public static void UpdatePrices(List<USPRoundPriceItem> items)

        {

            if (_uspRoundPrices == null)

            {

                _uspRoundPrices = new Dictionary<string, Dictionary<string, double>>();

            }

            _uspRoundPrices.Clear();

            foreach (var item in items)

            {

                if (!_uspRoundPrices.ContainsKey(item.Category))

                {

                    _uspRoundPrices[item.Category] = new Dictionary<string, double>();

                }

                _uspRoundPrices[item.Category][item.Subcategory] = item.Price;

            }

            SavePrices();

        }

        public static async Task<bool> SavePricesAsync(List<USPRoundPriceItem> items, USPRoundCoefficients coefficients, USPRoundFixedValues fixedValues)

        {

            try

            {

                if (_isOnlineMode)

                {

                    UpdatePrices(items);

                    UpdateCoefficients(coefficients);

                    UpdateFixedValues(fixedValues);

                    var uspRoundPricesForSupabase = new Dictionary<string, Dictionary<string, double>>();

                    foreach (var item in items)

                    {

                        if (!uspRoundPricesForSupabase.ContainsKey(item.Category))

                        {

                            uspRoundPricesForSupabase[item.Category] = new Dictionary<string, double>();

                        }

                        uspRoundPricesForSupabase[item.Category][item.Subcategory] = item.Price;

                    }

                    var pricesSuccess = await SupabaseUSPRoundPriceManager.SaveUSPRoundPricesToSupabase(uspRoundPricesForSupabase);

                    var coefficientsSuccess = await SupabaseUSPRoundPriceManager.SaveUSPRoundCoefficientsToSupabase(coefficients);

                    var fixedValuesSuccess = await SupabaseUSPRoundPriceManager.SaveUSPRoundFixedValuesToSupabase(fixedValues);

                    if (pricesSuccess && coefficientsSuccess && fixedValuesSuccess)

                    {

                        SavePrices();

                        SaveCoefficients();

                        SaveFixedValues();

                        return true;

                    }

                }

                else

                {

                    UpdatePrices(items);

                    UpdateCoefficients(coefficients);

                    UpdateFixedValues(fixedValues);

                    SavePrices();

                    SaveCoefficients();

                    SaveFixedValues();

                    return true;

                }

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Ошибка сохранения в облако: {ex.Message}\nСохраняем локально.",

                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                UpdatePrices(items);

                UpdateCoefficients(coefficients);

                UpdateFixedValues(fixedValues);

                SavePrices();

                SaveCoefficients();

                SaveFixedValues();

                return true;

            }

            return false;

        }

        public static void ResetToDefaults()

        {

            InitializeDefaultPrices();

            InitializeDefaultCoefficients();

            InitializeDefaultFixedValues();

            SavePrices();

            SaveCoefficients();

            SaveFixedValues();

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
