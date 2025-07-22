
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Configuration;
using Newtonsoft.Json;

namespace CalculatorApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<CoverageItem> _coverageItems;
        private ObservableCollection<PriceItem> _priceItems;

        public ObservableCollection<CoverageItem> CoverageItems => _coverageItems;
        public bool HasItems => _coverageItems?.Count > 0;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация коллекции
            _coverageItems = new ObservableCollection<CoverageItem>();

            // Подписка на изменения коллекции
            _coverageItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasItems));
            };

            // Инициализация системы паролей и цен
            PasswordManager.Initialize();
            PriceManager.LoadPrices();

            // Добавляем один расчет по умолчанию
            AddNewCalculation();

            // Загружаем цены для админ-панели
            LoadPricesForAdmin();

            DataContext = this;
        }

        #region Навигация

        private void NavigateToCalculator(object sender, RoutedEventArgs e)
        {
            SetActivePage("Calculator");
        }

        private void NavigateToAdmin(object sender, RoutedEventArgs e)
        {
            // Проверяем пароль
            if (!ValidateAdminPassword())
                return;

            LoadPricesForAdmin();
            SetActivePage("Admin");
        }

        private void SetActivePage(string pageName)
        {
            // Скрываем все страницы
            CalculatorPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Collapsed;

            // Сбрасываем активные кнопки
            CalculatorNavButton.Tag = null;
            AdminNavButton.Tag = null;

            // Показываем нужную страницу и активируем кнопку
            switch (pageName)
            {
                case "Calculator":
                    CalculatorPage.Visibility = Visibility.Visible;
                    CalculatorNavButton.Tag = "Active";
                    break;
                case "Admin":
                    AdminPage.Visibility = Visibility.Visible;
                    AdminNavButton.Tag = "Active";
                    break;
            }
        }

        #endregion

        #region Калькулятор

        private void AddNewCalculation()
        {
            var newItem = new CoverageItem();
            _coverageItems.Add(newItem);
        }

        private void AddCalculation_Click(object sender, RoutedEventArgs e)
        {
            AddNewCalculation();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить все расчеты?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _coverageItems.Clear();
                AddNewCalculation(); // Добавляем один пустой расчет
            }
        }

        private void DeleteCalculation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is CoverageItem item)
            {
                // Если это единственный расчет, не удаляем
                if (_coverageItems.Count <= 1)
                {
                    MessageBox.Show("Нельзя удалить единственный расчет. Добавьте новый расчет перед удалением текущего.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("Удалить этот расчет?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _coverageItems.Remove(item);
                }
            }
        }

        private void CopyResult_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is CoverageItem item)
            {
                if (item.HasError)
                {
                    Clipboard.SetText(item.ErrorMessage);
                }
                else
                {
                    Clipboard.SetText($"{item.FinalCost:F0}");
                }

                // Показываем уведомление
                ShowCopyNotification(button);
            }
        }

        private async void ShowCopyNotification(Button button)
        {
            // Создаем уведомление
            var notification = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 20, 0, 0),
                Opacity = 0
            };

            var textBlock = new TextBlock
            {
                Text = "✓ Скопировано в буфер обмена!",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            notification.Child = textBlock;

            var mainGrid = (Grid)this.Content;
            if (mainGrid == null) return;

            mainGrid.Children.Add(notification);
            Grid.SetColumnSpan(notification, 2);

            // Анимация появления
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            notification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);

            // Ждем 2.5 секунды
            await Task.Delay(2500);

            // Анимация исчезновения
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            fadeOutAnimation.Completed += (s, e) =>
            {
                mainGrid.Children.Remove(notification);
            };

            notification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        }

        #endregion

        #region Администрирование

        private bool ValidateAdminPassword()
        {
            var loginWindow = new AdminLoginWindow();
            loginWindow.Owner = this;

            var result = loginWindow.ShowDialog();
            return result == true;
        }

        private void LoadPricesForAdmin()
        {
            var prices = PriceManager.GetAllPrices();
            _priceItems = new ObservableCollection<PriceItem>(prices);
            PriceItemsControl.ItemsSource = _priceItems;
        }

        private void SavePrices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем валидность данных
                var invalidItems = _priceItems.Where(p => p.Price <= 0 ||
                                                        string.IsNullOrWhiteSpace(p.Type) ||
                                                        string.IsNullOrWhiteSpace(p.Thickness)).ToList();

                if (invalidItems.Any())
                {
                    MessageBox.Show("Обнаружены некорректные данные. Проверьте, что все цены больше 0 и все поля заполнены.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем цены
                PriceManager.UpdatePrices(_priceItems.ToList());
                PriceManager.SavePrices();

                MessageBox.Show("Цены успешно сохранены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем существующие расчеты
                foreach (var item in _coverageItems)
                {
                    item.RefreshPrices();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetPrices_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите сбросить все цены к значениям по умолчанию? Это действие нельзя отменить.",
                "Подтверждение сброса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    PriceManager.ResetToDefaults();
                    LoadPricesForAdmin();

                    MessageBox.Show("Цены сброшены к значениям по умолчанию.", "Сброс выполнен",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сбросе: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Смена пароля

        private void ToggleCurrentPasswordVisibility(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(
                CurrentPasswordBox,
                CurrentPasswordTextBox,
                CurrentEyeClosedIcon,
                CurrentEyeOpenIcon
            );
        }

        private void ToggleNewPasswordVisibility(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(
                NewPasswordBox,
                NewPasswordTextBox,
                NewEyeClosedIcon,
                NewEyeOpenIcon
            );
        }

        private void ToggleConfirmPasswordVisibility(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility(
                ConfirmPasswordBox,
                ConfirmPasswordTextBox,
                ConfirmEyeClosedIcon,
                ConfirmEyeOpenIcon
            );
        }

        private void TogglePasswordVisibility(PasswordBox passwordBox, TextBox textBox, UIElement eyeClosedIcon, UIElement eyeOpenIcon)
        {
            if (passwordBox.Visibility == Visibility.Visible)
            {
                // Переключаем на TextBox для показа пароля
                textBox.Text = passwordBox.Password;
                passwordBox.Visibility = Visibility.Collapsed;
                textBox.Visibility = Visibility.Visible;
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;

                // Меняем иконку на открытый глаз
                eyeClosedIcon.Visibility = Visibility.Collapsed;
                eyeOpenIcon.Visibility = Visibility.Visible;
            }
            else
            {
                // Переключаем на PasswordBox для сокрытия пароля
                passwordBox.Password = textBox.Text;
                textBox.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;
                passwordBox.Focus();

                // Меняем иконку на закрытый глаз
                eyeOpenIcon.Visibility = Visibility.Collapsed;
                eyeClosedIcon.Visibility = Visibility.Visible;
            }
        }

        private void ChangePasswordClick(object sender, RoutedEventArgs e)
        {
            ClearErrorMessage();

            // Получаем пароли с учетом текущего состояния (видимый/скрытый)
            var currentPassword = CurrentPasswordBox.Visibility == Visibility.Visible
                ? CurrentPasswordBox.Password
                : CurrentPasswordTextBox.Text;

            var newPassword = NewPasswordBox.Visibility == Visibility.Visible
                ? NewPasswordBox.Password
                : NewPasswordTextBox.Text;

            var confirmPassword = ConfirmPasswordBox.Visibility == Visibility.Visible
                ? ConfirmPasswordBox.Password
                : ConfirmPasswordTextBox.Text;

            // Проверка заполненности полей
            if (string.IsNullOrEmpty(currentPassword))
            {
                ShowErrorMessage("Введите текущий пароль");
                CurrentPasswordBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ShowErrorMessage("Введите новый пароль");
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword.Length < 3)
            {
                ShowErrorMessage("Новый пароль должен содержать минимум 3 символа");
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowErrorMessage("Пароли не совпадают");
                ConfirmPasswordBox.Focus();
                return;
            }

            // Пытаемся изменить пароль
            bool success = PasswordManager.ChangePassword(currentPassword, newPassword);

            if (success)
            {
                MessageBox.Show("Пароль успешно изменен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearPasswordFields();
            }
            else
            {
                ShowErrorMessage("Неверный текущий пароль");
                CurrentPasswordBox.Clear();
                CurrentPasswordBox.Focus();
            }
        }

        private void ResetPasswordClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Сбросить пароль к значению по умолчанию '{PasswordManager.GetDefaultPassword()}'?",
                "Подтверждение сброса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PasswordManager.ResetToDefault();
                MessageBox.Show($"Пароль сброшен к значению по умолчанию: {PasswordManager.GetDefaultPassword()}",
                    "Пароль сброшен", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearPasswordFields();
            }
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private void ClearErrorMessage()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void ClearPasswordFields()
        {
            // Очищаем и сбрасываем все поля паролей
            CurrentPasswordBox.Clear();
            CurrentPasswordTextBox.Clear();
            NewPasswordBox.Clear();
            NewPasswordTextBox.Clear();
            ConfirmPasswordBox.Clear();
            ConfirmPasswordTextBox.Clear();

            // Сбрасываем видимость всех полей к скрытому состоянию
            CurrentPasswordBox.Visibility = Visibility.Visible;
            CurrentPasswordTextBox.Visibility = Visibility.Collapsed;
            CurrentEyeClosedIcon.Visibility = Visibility.Visible;
            CurrentEyeOpenIcon.Visibility = Visibility.Collapsed;

            NewPasswordBox.Visibility = Visibility.Visible;
            NewPasswordTextBox.Visibility = Visibility.Collapsed;
            NewEyeClosedIcon.Visibility = Visibility.Visible;
            NewEyeOpenIcon.Visibility = Visibility.Collapsed;

            ConfirmPasswordBox.Visibility = Visibility.Visible;
            ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
            ConfirmEyeClosedIcon.Visibility = Visibility.Visible;
            ConfirmEyeOpenIcon.Visibility = Visibility.Collapsed;

            ClearErrorMessage();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region Модели данных и менеджеры

    public class CoverageItem : INotifyPropertyChanged
    {
        private string _type;
        private object _thickness;
        private double _area;
        private double _basePrice;
        private double _finalCost;
        private string _region;
        private string _errorMessage;
        private bool _hasError;

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(AvailableThicknesses));

                _thickness = null;
                OnPropertyChanged(nameof(Thickness));

                UpdateBasePrice();
                Calculate();
            }
        }

        public object Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                OnPropertyChanged(nameof(Thickness));
                UpdateBasePrice();
                Calculate();
            }
        }

        public object[] AvailableThicknesses => PriceManager.GetAvailableThicknesses(_type);
        public string[] AvailableTypes => PriceManager.GetAvailableTypes().ToArray();
        public string[] AvailableRegions => new[] { "Москва", "МО", "Другой регион" };

        public double Area
        {
            get => _area;
            set
            {
                _area = value;
                OnPropertyChanged(nameof(Area));
                Calculate();
            }
        }

        public string Region
        {
            get => _region;
            set
            {
                _region = value;
                OnPropertyChanged(nameof(Region));
                Calculate();
            }
        }

        public double BasePrice
        {
            get => _basePrice;
            private set
            {
                _basePrice = value;
                OnPropertyChanged(nameof(BasePrice));
            }
        }

        public double FinalCost
        {
            get => _finalCost;
            private set
            {
                _finalCost = value;
                OnPropertyChanged(nameof(FinalCost));
                OnPropertyChanged(nameof(FinalPricePerSquareMeter));
            }
        }

        public double FinalPricePerSquareMeter
        {
            get
            {
                if (Area <= 0 || FinalCost <= 0)
                    return BasePrice;
                return FinalCost / Area;
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                _hasError = value;
                OnPropertyChanged(nameof(HasError));
            }
        }

        public CoverageItem()
        {
            Type = "";
            Thickness = null;
            Area = 0;
            Region = "Москва";
            BasePrice = 0;
            FinalCost = 0;
            ErrorMessage = "";
            HasError = false;
        }

        public void RefreshPrices()
        {
            UpdateBasePrice();
            Calculate();
            OnPropertyChanged(nameof(AvailableThicknesses));
        }

        private void UpdateBasePrice()
        {
            if (string.IsNullOrEmpty(Type) || Thickness == null)
            {
                BasePrice = 0;
                return;
            }

            string thicknessStr = Thickness.ToString();
            BasePrice = PriceManager.GetPrice(Type, thicknessStr);
        }

        private void Calculate()
        {
            HasError = false;
            ErrorMessage = "";

            if (Region != "Москва" && Region != "МО")
            {
                HasError = true;
                ErrorMessage = "Т.к. выбран другой регион, необходимо связаться с ответственным лицом для детального анализа стоимости";
                FinalCost = 0;
                return;
            }

            if (Area < 50 && Area > 0)
            {
                HasError = true;
                ErrorMessage = "Т.к. площадь покрытия меньше 50м², необходимо связаться с ответственным лицом для детального анализа стоимости";
                FinalCost = 0;
                return;
            }

            if (Area <= 0 || BasePrice <= 0)
            {
                FinalCost = 0;
                return;
            }

            double cost = Area * BasePrice;

            if (Area >= 50 && Area < 70)
            {
                cost *= 3.0;
            }
            else if (Area >= 70 && Area < 100)
            {
                cost *= 2.0;
            }
            else if (Area >= 100 && Area <= 120)
            {
                cost *= 1.2;
            }

            FinalCost = cost;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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

    public static class PriceManager
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
                {
                    "Обычное цвет красный/зеленый", new Dictionary<string, double>
                    {
                        { "10", 1650 },
                        { "15", 2400 },
                        { "20", 3000 },
                        { "30", 4400 },
                        { "40", 5800 },
                        { "50", 7500 }
                    }
                },
                {
                    "Обычное цвет синий/желтый", new Dictionary<string, double>
                    {
                        { "10", 1815 },
                        { "15", 2640 },
                        { "20", 3300 },
                        { "30", 4840 },
                        { "40", 6380 },
                        { "50", 8250 }
                    }
                },
                {
                    "ЕПДМ", new Dictionary<string, double>
                    {
                        { "10", 3000 },
                        { "10+10", 3900 },
                        { "20+10", 5650 },
                        { "30+10", 6100 },
                        { "40+10", 7600 }
                    }
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
            if (_prices.ContainsKey(type) && _prices[type].ContainsKey(thickness))
            {
                return _prices[type][thickness];
            }
            return 0;
        }

        public static List<PriceItem> GetAllPrices()
        {
            var items = new List<PriceItem>();

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

            return items;
        }

        public static void UpdatePrices(List<PriceItem> items)
        {
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
            return new List<string>(_prices.Keys);
        }

        public static object[] GetAvailableThicknesses(string type)
        {
            if (string.IsNullOrEmpty(type) || !_prices.ContainsKey(type))
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

    public static class PasswordManager
    {
        // ВСТРОЕННЫЙ ПАРОЛЬ ПО УМОЛЧАНИЮ - ИЗМЕНИТЕ ЭТО ЗНАЧЕНИЕ ДЛЯ СОЗДАНИЯ КАСТОМНОЙ ВЕРСИИ
        private const string DEFAULT_EMBEDDED_PASSWORD = "admin123";    // ← Измените этот пароль

        // Сохраняем пароль в папку данных пользователя (AppData)
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiKom Calculator"
        );

        private static readonly string PasswordFile = Path.Combine(AppDataFolder, ".Fi8Hhc80jbT2c9c1");

        private static string _currentPasswordHash;

        public static void Initialize()
        {
            try
            {
                // Создаем папку данных приложения если её нет
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                // Загружаем хеш пароля из скрытого файла
                if (File.Exists(PasswordFile))
                {
                    var savedHash = File.ReadAllText(PasswordFile).Trim();
                    if (!string.IsNullOrEmpty(savedHash))
                    {
                        _currentPasswordHash = savedHash;
                    }
                    else
                    {
                        // Файл пуст, создаем хеш по умолчанию
                        _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
                        SavePasswordHash();
                    }
                }
                else
                {
                    // При первом запуске сохраняем хеш пароля по умолчанию
                    _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
                    SavePasswordHash();
                }
            }
            catch
            {
                // В случае ошибки используем пароль по умолчанию
                _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
            }
        }

        public static bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            var hashToCheck = HashPassword(password);
            return hashToCheck == _currentPasswordHash;
        }

        public static bool ChangePassword(string currentPassword, string newPassword)
        {
            if (!ValidatePassword(currentPassword))
                return false;

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 3)
                return false;

            try
            {
                // Обновляем хеш в памяти
                _currentPasswordHash = HashPassword(newPassword);

                // Сохраняем зашифрованный хеш в файл
                SavePasswordHash();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ResetToDefault()
        {
            try
            {
                // Сбрасываем к хешу пароля по умолчанию
                _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);

                // Сохраняем зашифрованный хеш в файл
                SavePasswordHash();
            }
            catch
            {
                // В случае ошибки используем пароль по умолчанию только в памяти
                _currentPasswordHash = HashPassword(DEFAULT_EMBEDDED_PASSWORD);
            }
        }

        private static void SavePasswordHash()
        {
            try
            {
                // Создаем папку данных приложения если её нет
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                // Сохраняем только зашифрованный хеш, а не сам пароль
                File.WriteAllText(PasswordFile, _currentPasswordHash);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пароля: {ex.Message}\n\nПуть: {PasswordFile}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CalculatorAppSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static string GetDefaultPassword()
        {
            return DEFAULT_EMBEDDED_PASSWORD;
        }

        public static string GetCurrentPassword()
        {
            // Мы не можем вернуть расшифрованный пароль из хеша,
            // поэтому возвращаем пароль по умолчанию для справки
            return DEFAULT_EMBEDDED_PASSWORD;
        }
    }

    #endregion

    // Окно авторизации администратора
    public partial class AdminLoginWindow : Window
    {
        private bool _isPasswordVisible = false;

        public AdminLoginWindow()
        {
            InitializeAdminLoginWindow();
        }

        private void InitializeAdminLoginWindow()
        {
            Title = "Авторизация администратора";
            Width = 400;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Заголовок
            var titleBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 165, 80)),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var titleText = new TextBlock
            {
                Text = "🔐 Вход в административную панель",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            titleBorder.Child = titleText;
            Grid.SetRow(titleBorder, 0);
            mainGrid.Children.Add(titleBorder);

            // Поле ввода пароля
            var passwordPanel = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 20)
            };

            var passwordLabel = new TextBlock
            {
                Text = "Пароль администратора:",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };

            passwordPanel.Children.Add(passwordLabel);

            // Контейнер для пароля с кнопкой
            var passwordContainer = new Grid();

            var passwordBox = new PasswordBox
            {
                Name = "AdminPasswordBox",
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(12, 0, 45, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var passwordTextBox = new TextBox
            {
                Name = "AdminPasswordTextBox",
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(12, 0, 45, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            var toggleButton = new Button
            {
                Width = 35,
                Height = 35,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Показать/скрыть пароль"
            };

            // Иконка глаза
            var eyeCanvas = new Canvas
            {
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Создаем закрытый глаз (по умолчанию)
            var eyePath = new System.Windows.Shapes.Path
            {
                Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)), // TextSecondaryBrush
                Data = Geometry.Parse("M1,8 C1,8 3.5,3 8,3 C12.5,3 15,8 15,8 C15,8 12.5,13 8,13 C3.5,13 1,8 1,8 Z")
            };

            var eyePupil = new System.Windows.Shapes.Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(6, 6, 0, 0)
            };

            var strikethrough = new System.Windows.Shapes.Line
            {
                X1 = 2,
                Y1 = 2,
                X2 = 14,
                Y2 = 14,
                Stroke = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                StrokeThickness = 1.5
            };

            eyeCanvas.Children.Add(eyePath);
            eyeCanvas.Children.Add(eyePupil);
            eyeCanvas.Children.Add(strikethrough);

            toggleButton.Content = eyeCanvas;

            passwordContainer.Children.Add(passwordBox);
            passwordContainer.Children.Add(passwordTextBox);
            passwordContainer.Children.Add(toggleButton);

            passwordPanel.Children.Add(passwordContainer);
            Grid.SetRow(passwordPanel, 1);
            mainGrid.Children.Add(passwordPanel);

            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var loginButton = new Button
            {
                Content = "Войти",
                Width = 100,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(0, 165, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            buttonPanel.Children.Add(loginButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;

            // Обработчики событий
            toggleButton.Click += (s, e) =>
            {
                if (!_isPasswordVisible)
                {
                    passwordTextBox.Text = passwordBox.Password;
                    passwordBox.Visibility = Visibility.Collapsed;
                    passwordTextBox.Visibility = Visibility.Visible;
                    passwordTextBox.Focus();
                    passwordTextBox.CaretIndex = passwordTextBox.Text.Length;

                    // Обновляем иконку - убираем линию зачеркивания
                    strikethrough.Visibility = Visibility.Collapsed;
                    _isPasswordVisible = true;
                }
                else
                {
                    passwordBox.Password = passwordTextBox.Text;
                    passwordTextBox.Visibility = Visibility.Collapsed;
                    passwordBox.Visibility = Visibility.Visible;
                    passwordBox.Focus();

                    // Обновляем иконку - показываем линию зачеркивания
                    strikethrough.Visibility = Visibility.Visible;
                    _isPasswordVisible = false;
                }
            };

            loginButton.Click += (s, e) =>
            {
                var password = _isPasswordVisible ? passwordTextBox.Text : passwordBox.Password;

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PasswordManager.ValidatePassword(password))
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Неверный пароль!", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (_isPasswordVisible)
                    {
                        passwordTextBox.Clear();
                        passwordTextBox.Focus();
                    }
                    else
                    {
                        passwordBox.Clear();
                        passwordBox.Focus();
                    }
                }
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            // Обработка Enter
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    loginButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else if (e.Key == Key.Escape)
                {
                    cancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };

            // Фокус на поле пароля при открытии
            Loaded += (s, e) => passwordBox.Focus();
        }
    }
}
