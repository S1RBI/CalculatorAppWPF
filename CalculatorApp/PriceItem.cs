using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CalculatorApp
{
    public class PriceItem : INotifyPropertyChanged
    {
        private string _type;
        private string _thickness;
        private double _price;

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public string Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                OnPropertyChanged(nameof(Thickness));
            }
        }

        public double Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static partial class PriceManager
    {
        // Сохраняем JSON в папку данных пользователя (AppData)
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiKom Calculator"
        );

        private static readonly string ConfigFile = Path.Combine(AppDataFolder, "prices.json");
        private static Dictionary<string, Dictionary<string, double>> _prices;

        public static void LoadPrices()
        {
            try
            {
                // Создаем папку данных приложения если её нет
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    _prices = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(json);
                }
                else
                {
                    InitializeDefaultPrices();
                    SavePrices(); // Сохраняем значения по умолчанию при первом запуске
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки цен: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeDefaultPrices();
            }
        }

        public static void ResetToDefaults()
        {
            InitializeDefaultPrices();
            SavePrices();
        }

        private static void InitializeDefaultPrices()
        {
            _prices = new Dictionary<string, Dictionary<string, double>>
            {
                ["Обычное цвет красный/зеленый"] = new Dictionary<string, double>
                {
                    ["10"] = 1650,
                    ["15"] = 2400,
                    ["20"] = 3000,
                    ["30"] = 4400,
                    ["40"] = 5800,
                    ["50"] = 7500
                },
                ["Обычное цвет синий/желтый"] = new Dictionary<string, double>
                {
                    ["10"] = 1815,
                    ["15"] = 2640,
                    ["20"] = 3300,
                    ["30"] = 4840,
                    ["40"] = 6380,
                    ["50"] = 8250
                },
                ["ЕПДМ"] = new Dictionary<string, double>
                {
                    ["10"] = 3000,
                    ["10+10"] = 3900,
                    ["20+10"] = 5650,
                    ["30+10"] = 6100,
                    ["40+10"] = 7600
                }
            };
        }

        public static void SavePrices()
        {
            try
            {
                // Создаем папку данных приложения если её нет
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var json = JsonConvert.SerializeObject(_prices, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения цен: {ex.Message}\n\nПуть: {ConfigFile}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static double GetPrice(string type, string thickness)
        {
            if (_prices != null && _prices.ContainsKey(type) && _prices[type].ContainsKey(thickness))
            {
                return _prices[type][thickness];
            }
            return 0;
        }

        public static List<PriceItem> GetAllPrices()
        {
            var items = new List<PriceItem>();

            if (_prices != null)
            {
                foreach (var type in _prices.Keys)
                {
                    foreach (var thickness in _prices[type].Keys)
                    {
                        items.Add(new PriceItem
                        {
                            Type = type,
                            Thickness = thickness,
                            Price = _prices[type][thickness]
                        });
                    }
                }
            }

            return items;
        }

        public static void UpdatePrices(List<PriceItem> items)
        {
            // Инициализируем _prices если он null
            if (_prices == null)
            {
                _prices = new Dictionary<string, Dictionary<string, double>>();
            }

            _prices.Clear();

            foreach (var item in items)
            {
                if (!_prices.ContainsKey(item.Type))
                {
                    _prices[item.Type] = new Dictionary<string, double>();
                }

                _prices[item.Type][item.Thickness] = item.Price;
            }
        }

        public static List<string> GetAvailableTypes()
        {
            if (_prices != null)
            {
                return new List<string>(_prices.Keys);
            }
            return new List<string>();
        }

        public static object[] GetAvailableThicknesses(string type)
        {
            if (string.IsNullOrEmpty(type) || _prices == null || !_prices.ContainsKey(type))
                return new object[0];

            var thicknesses = _prices[type].Keys.ToList();

            if (type.Equals("ЕПДМ", StringComparison.OrdinalIgnoreCase))
            {
                return thicknesses.Cast<object>().ToArray();
            }
            else
            {
                return thicknesses.Select(t => double.TryParse(t, out var val) ? (object)val : (object)t).ToArray();
            }
        }
    }
}
