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
using System.Windows.Data;
using System.Net.Http;
using System.Windows.Markup;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Effects;

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

            // Асинхронная инициализация с поддержкой облака
            _ = InitializeAsync();

            DataContext = this;
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Пытаемся подключиться к Supabase
                await PriceManager.InitializeWithCloudAsync();

                // Обновляем заголовок окна с информацией о режиме
                this.Title = $"Калькулятор покрытий - {PriceManager.GetModeString()}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}\nРаботаем в локальном режиме.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Fallback к локальному режиму
                PriceManager.LoadPrices();
                this.Title = "Калькулятор покрытий - Локальный режим";
            }

            // Добавляем один расчет по умолчанию
            AddNewCalculation();

            // Загружаем цены для админ-панели
            LoadPricesForAdmin();
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
            // Только облачная аутентификация
            var loginWindow = new AdminLoginWindow();
            loginWindow.Owner = this;
            var result = loginWindow.ShowDialog();
            return result == true;
        }

        // Методы для управления видимостью паролей
        private void ToggleCurrentPasswordVisibility(object sender, RoutedEventArgs e)
        {
            var isVisible = CurrentPasswordTextBox.Visibility == Visibility.Visible;

            if (isVisible)
            {
                CurrentPasswordBox.Password = CurrentPasswordTextBox.Text;
                CurrentPasswordTextBox.Visibility = Visibility.Collapsed;
                CurrentPasswordBox.Visibility = Visibility.Visible;
                CurrentPasswordBox.Focus();

                CurrentEyeOpenIcon.Visibility = Visibility.Collapsed;
                CurrentEyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                CurrentPasswordTextBox.Text = CurrentPasswordBox.Password;
                CurrentPasswordBox.Visibility = Visibility.Collapsed;
                CurrentPasswordTextBox.Visibility = Visibility.Visible;
                CurrentPasswordTextBox.Focus();
                CurrentPasswordTextBox.CaretIndex = CurrentPasswordTextBox.Text.Length;

                CurrentEyeClosedIcon.Visibility = Visibility.Collapsed;
                CurrentEyeOpenIcon.Visibility = Visibility.Visible;
            }
        }

        private void ToggleNewPasswordVisibility(object sender, RoutedEventArgs e)
        {
            var isVisible = NewPasswordTextBox.Visibility == Visibility.Visible;

            if (isVisible)
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
                NewPasswordTextBox.Visibility = Visibility.Collapsed;
                NewPasswordBox.Visibility = Visibility.Visible;
                NewPasswordBox.Focus();

                NewEyeOpenIcon.Visibility = Visibility.Collapsed;
                NewEyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                NewPasswordTextBox.Text = NewPasswordBox.Password;
                NewPasswordBox.Visibility = Visibility.Collapsed;
                NewPasswordTextBox.Visibility = Visibility.Visible;
                NewPasswordTextBox.Focus();
                NewPasswordTextBox.CaretIndex = NewPasswordTextBox.Text.Length;

                NewEyeClosedIcon.Visibility = Visibility.Collapsed;
                NewEyeOpenIcon.Visibility = Visibility.Visible;
            }
        }

        private void ToggleConfirmPasswordVisibility(object sender, RoutedEventArgs e)
        {
            var isVisible = ConfirmPasswordTextBox.Visibility == Visibility.Visible;

            if (isVisible)
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Focus();

                ConfirmEyeOpenIcon.Visibility = Visibility.Collapsed;
                ConfirmEyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                ConfirmPasswordTextBox.Focus();
                ConfirmPasswordTextBox.CaretIndex = ConfirmPasswordTextBox.Text.Length;

                ConfirmEyeClosedIcon.Visibility = Visibility.Collapsed;
                ConfirmEyeOpenIcon.Visibility = Visibility.Visible;
            }
        }

        // Метод смены пароля (работает с паролем Supabase)
        private async void ChangePasswordClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorMessage.Visibility = Visibility.Collapsed;

                var currentPassword = CurrentPasswordTextBox.Visibility == Visibility.Visible
                    ? CurrentPasswordTextBox.Text
                    : CurrentPasswordBox.Password;

                var newPassword = NewPasswordTextBox.Visibility == Visibility.Visible
                    ? NewPasswordTextBox.Text
                    : NewPasswordBox.Password;

                var confirmPassword = ConfirmPasswordTextBox.Visibility == Visibility.Visible
                    ? ConfirmPasswordTextBox.Text
                    : ConfirmPasswordBox.Password;

                // Проверки
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ShowPasswordError("Введите текущий пароль");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ShowPasswordError("Введите новый пароль");
                    return;
                }

                if (newPassword.Length < 6)
                {
                    ShowPasswordError("Новый пароль должен содержать минимум 6 символов");
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    ShowPasswordError("Пароли не совпадают");
                    return;
                }

                if (newPassword == currentPassword)
                {
                    ShowPasswordError("Новый пароль должен отличаться от текущего");
                    return;
                }

                // Показываем индикатор загрузки
                var loadingWindow = ShowLoadingWindow("Смена пароля...");

                try
                {
                    // Сначала проверяем текущий пароль, повторно войдя в систему
                    var currentEmail = await SupabaseAuthManager.GetCurrentUserEmailAsync();
                    if (string.IsNullOrEmpty(currentEmail))
                    {
                        ShowPasswordError("Ошибка: пользователь не авторизован");
                        return;
                    }

                    // Пытаемся войти с текущим паролем для проверки
                    var currentPasswordValid = await SupabaseAuthManager.SignInAsync(currentEmail, currentPassword);

                    if (!currentPasswordValid)
                    {
                        ShowPasswordError("Неверный текущий пароль");
                        return;
                    }

                    // Меняем пароль в Supabase
                    var success = await SupabaseAuthManager.UpdatePasswordAsync(newPassword);

                    if (success)
                    {
                        MessageBox.Show("Пароль успешно изменен в базе данных!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Очищаем поля
                        ClearPasswordFields();
                    }
                    else
                    {
                        ShowPasswordError("Ошибка при смене пароля в базе данных");
                    }
                }
                catch (Exception ex)
                {
                    ShowPasswordError($"Ошибка при смене пароля: {ex.Message}");
                }
                finally
                {
                    loadingWindow.Close();
                }
            }
            catch (Exception ex)
            {
                ShowPasswordError($"Ошибка: {ex.Message}");
            }
        }

        // Информация о смене пароля в Supabase
        private void ResetPasswordClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var message = "Сброс пароля Supabase необходимо выполнить через:\n\n" +
                             "1. Панель управления Supabase (Dashboard)\n" +
                             "2. Функцию восстановления пароля по email\n" +
                             "3. SQL команды в редакторе базы данных\n\n" +
                             "Данная функция предназначена для управления паролем в облачной базе данных.";

                MessageBox.Show(message, "Информация о сбросе пароля",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowPasswordError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowPasswordError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private async void ShowCurrentPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentEmail = await SupabaseAuthManager.GetCurrentUserEmailAsync();
                var message = $"По всем вопросам и правкам обращаться на почту: {currentEmail}\n\n" +
                             "Это пароль для входа в базу данных Supabase.\n" +
                             "Для смены пароля используйте форму выше.\n\n" +
                             "Примечание: Минимальная длина пароля в Supabase: 6 символов";

                MessageBox.Show(message, "Информация о пароле Supabase",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения информации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPasswordFields()
        {
            CurrentPasswordBox.Clear();
            CurrentPasswordTextBox.Clear();
            NewPasswordBox.Clear();
            NewPasswordTextBox.Clear();
            ConfirmPasswordBox.Clear();
            ConfirmPasswordTextBox.Clear();
        }



        private void LoadPricesForAdmin()
        {
            var prices = PriceManager.GetAllPrices();
            _priceItems = new ObservableCollection<PriceItem>(prices);
            PriceItemsControl.ItemsSource = _priceItems;
        }

        private async void SavePrices_Click(object sender, RoutedEventArgs e)
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

                // Показываем индикатор загрузки
                var loadingWindow = ShowLoadingWindow("Сохранение данных...");

                try
                {
                    // Сохраняем цены
                    PriceManager.UpdatePrices(_priceItems.ToList());
                    var success = await PriceManager.SavePricesAsync();

                    loadingWindow.Close();

                    if (success)
                    {
                        var mode = PriceManager.IsOnlineMode() ? "в облаке и локально" : "локально";
                        MessageBox.Show($"Цены успешно сохранены {mode}!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем существующие расчеты
                        foreach (var item in _coverageItems)
                        {
                            item.RefreshPrices();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка сохранения данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception saveEx)
                {
                    loadingWindow.Close();
                    throw saveEx;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Window ShowLoadingWindow(string message)
        {
            var loadingWindow = new Window
            {
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                Background = new SolidColorBrush(Colors.White),
                Content = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            FontSize = 16,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 20)
                        },
                        new System.Windows.Controls.ProgressBar
                        {
                            IsIndeterminate = true,
                            Width = 200,
                            Height = 20
                        }
                    }
                }
            };

            loadingWindow.Show();
            return loadingWindow;
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

        #region Информация о системе

        private void ShowSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            var mode = PriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
            MessageBox.Show($"Текущий режим работы: {mode}\n\nВерсия данных: {SupabasePriceManager.GetCurrentVersion()}",
                "Информация о системе", MessageBoxButton.OK, MessageBoxImage.Information);
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
                OnPropertyChanged(nameof(CoefficientDisplay));
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
                OnPropertyChanged(nameof(CoefficientDisplay));
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
                OnPropertyChanged(nameof(CoefficientDisplay));
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

        public string CoefficientDisplay
        {
            get
            {
                if (Area <= 0 || BasePrice <= 0 || HasError)
                    return "";

                if (Area >= 50 && Area < 70)
                    return "(x3)";
                else if (Area >= 70 && Area < 100)
                    return "(x2)";
                else if (Area >= 100 && Area < 120)
                    return "(x1.2)";
                else
                    return "";
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
                OnPropertyChanged(nameof(CoefficientDisplay));
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
            OnPropertyChanged(nameof(CoefficientDisplay));
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
            else if (Area >= 100 && Area < 120)
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

    public static class PasswordManager
    {
        // ВСТРОЕННЫЙ ПАРОЛЬ ПО УМОЛЧАНИЮ - ИЗМЕНИТЕ ЭТО ЗНАЧЕНИЕ ДЛЯ СОЗДАНИЯ КАСТОМНОЙ ВЕРСИИ
        private const string DEFAULT_EMBEDDED_PASSWORD = "admin123"; // ← Измените этот пароль

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

    public class WidthToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width <= 960;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



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
            Title = "Вход в админ-панель";
            Width = 420;
            Height = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            // Основная граница с тенью
            var mainBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.15,
                    BlurRadius = 20,
                    ShadowDepth = 8
                }
            };

            // Возможность перетаскивания окна
            mainBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Заголовок
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Поле пароля
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Кнопки
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Сообщение об ошибке
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Пространство

            // Заголовок с иконкой
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(32, 24, 32, 20)
            };

            // Иконка безопасности
            var iconBorder = new Border
            {
                Width = 48,
                Height = 48,
                Background = new LinearGradientBrush(
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(168, 85, 247),
                    90),
                CornerRadius = new CornerRadius(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var iconText = new TextBlock
            {
                Text = "🔐",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };

            iconBorder.Child = iconText;
            titlePanel.Children.Add(iconBorder);

            var titleText = new TextBlock
            {
                Text = "Административная панель",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var subtitleText = new TextBlock
            {
                Text = "Введите пароль для продолжения",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            titlePanel.Children.Add(titleText);
            titlePanel.Children.Add(subtitleText);

            Grid.SetRow(titlePanel, 0);
            mainGrid.Children.Add(titlePanel);

            // Поле ввода пароля
            var passwordPanel = new StackPanel
            {
                Margin = new Thickness(32, 0, 32, 20)
            };

            var passwordLabel = new TextBlock
            {
                Text = "Пароль",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };

            passwordPanel.Children.Add(passwordLabel);

            // Контейнер для пароля с кнопкой
            var passwordContainer = new Grid
            {
                Height = 48
            };

            var passwordBox = new PasswordBox
            {
                Name = "AdminPasswordBox",
                Height = 48,
                FontSize = 15,
                Padding = new Thickness(16, 0, 50, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(2, 2, 2, 2),
                Background = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center,
                PasswordChar = '●'
            };

            // Стили для фокуса
            passwordBox.GotFocus += (s, e) =>
            {
                passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            };
            passwordBox.LostFocus += (s, e) =>
            {
                passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
            };

            var passwordTextBox = new TextBox
            {
                Name = "AdminPasswordTextBox",
                Height = 48,
                FontSize = 15,
                Padding = new Thickness(16, 0, 50, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(2, 2, 2, 2),
                Background = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            // Стили для фокуса
            passwordTextBox.GotFocus += (s, e) =>
            {
                passwordTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            };
            passwordTextBox.LostFocus += (s, e) =>
            {
                passwordTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
            };

            var toggleButton = new Button
            {
                Width = 40,
                Height = 40,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Показать/скрыть пароль"
            };

            // Эффект наведения
            toggleButton.MouseEnter += (s, e) =>
            {
                toggleButton.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246));
            };
            toggleButton.MouseLeave += (s, e) =>
            {
                toggleButton.Background = Brushes.Transparent;
            };

            // Иконка глаза (векторная)
            var eyeGrid = new Grid
            {
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Закрытый глаз (по умолчанию)
            var eyeClosedCanvas = new Canvas
            {
                Name = "EyeClosedIcon",
                Visibility = Visibility.Visible
            };

            // Глаз
            var eyePath = new System.Windows.Shapes.Path
            {
                Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Data = Geometry.Parse("M1,8 C1,8 3.5,3 8,3 C12.5,3 15,8 15,8 C15,8 12.5,13 8,13 C3.5,13 1,8 1,8 Z")
            };

            // Зрачок
            var eyePupil = new System.Windows.Shapes.Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            Canvas.SetLeft(eyePupil, 6);
            Canvas.SetTop(eyePupil, 6);

            // Линия перечеркивания
            var strikethrough = new System.Windows.Shapes.Line
            {
                X1 = 2,
                Y1 = 2,
                X2 = 14,
                Y2 = 14,
                Stroke = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                StrokeThickness = 1.5
            };

            eyeClosedCanvas.Children.Add(eyePath);
            eyeClosedCanvas.Children.Add(eyePupil);
            eyeClosedCanvas.Children.Add(strikethrough);

            // Открытый глаз
            var eyeOpenCanvas = new Canvas
            {
                Name = "EyeOpenIcon",
                Visibility = Visibility.Collapsed
            };

            // Глаз
            var eyePathOpen = new System.Windows.Shapes.Path
            {
                Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Data = Geometry.Parse("M1,8 C1,8 3.5,3 8,3 C12.5,3 15,8 15,8 C15,8 12.5,13 8,13 C3.5,13 1,8 1,8 Z")
            };

            // Белок глаза
            var eyeWhite = new System.Windows.Shapes.Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.White
            };
            Canvas.SetLeft(eyeWhite, 5);
            Canvas.SetTop(eyeWhite, 5);

            // Зрачок
            var eyePupilOpen = new System.Windows.Shapes.Ellipse
            {
                Width = 3,
                Height = 3,
                Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            Canvas.SetLeft(eyePupilOpen, 6.5);
            Canvas.SetTop(eyePupilOpen, 6.5);

            eyeOpenCanvas.Children.Add(eyePathOpen);
            eyeOpenCanvas.Children.Add(eyeWhite);
            eyeOpenCanvas.Children.Add(eyePupilOpen);

            eyeGrid.Children.Add(eyeClosedCanvas);
            eyeGrid.Children.Add(eyeOpenCanvas);

            toggleButton.Content = eyeGrid;

            passwordContainer.Children.Add(passwordBox);
            passwordContainer.Children.Add(passwordTextBox);
            passwordContainer.Children.Add(toggleButton);

            // Создаем стили со скругленными углами ДО применения
            var passwordBoxStyle = new Style(typeof(PasswordBox));
            passwordBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, CreateRoundedPasswordBoxTemplate()));
            passwordBox.Style = passwordBoxStyle;

            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, CreateRoundedTextBoxTemplate()));
            passwordTextBox.Style = textBoxStyle;

            passwordPanel.Children.Add(passwordContainer);

            Grid.SetRow(passwordPanel, 1);
            mainGrid.Children.Add(passwordPanel);

            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(32, 0, 32, 20)
            };

            var loginButton = new Button
            {
                Content = "🔓 Войти",
                Width = 140,
                Height = 48,
                Background = new LinearGradientBrush(
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(168, 85, 247),
                    90),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 12, 0),
                Cursor = Cursors.Hand
            };

            // Создаем стиль для кнопки входа ДО применения
            var loginButtonStyle = new Style(typeof(Button));
            loginButtonStyle.Setters.Add(new Setter(Control.TemplateProperty, CreateRoundedButtonTemplate()));
            loginButton.Style = loginButtonStyle;

            loginButton.MouseEnter += (s, e) =>
            {
                loginButton.Background = new SolidColorBrush(Color.FromRgb(124, 58, 237));
            };
            loginButton.MouseLeave += (s, e) =>
            {
                loginButton.Background = new LinearGradientBrush(
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(168, 85, 247),
                    90);
            };

            var cancelButton = new Button
            {
                Content = "✕ Отмена",
                Width = 120,
                Height = 48,
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            // Создаем стиль для кнопки отмены ДО применения
            var cancelButtonStyle = new Style(typeof(Button));
            cancelButtonStyle.Setters.Add(new Setter(Control.TemplateProperty, CreateRoundedButtonTemplate()));
            cancelButton.Style = cancelButtonStyle;

            cancelButton.MouseEnter += (s, e) =>
            {
                cancelButton.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
            };
            cancelButton.MouseLeave += (s, e) =>
            {
                cancelButton.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246));
            };

            buttonPanel.Children.Add(loginButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            mainBorder.Child = mainGrid;
            Content = mainBorder;

            // Переменная для отслеживания состояния загрузки
            var isLoading = false;

            // Обработчики событий
            toggleButton.Click += (s, e) =>
            {
                var eyeGrid = (Grid)toggleButton.Content;
                var eyeClosedIcon = eyeGrid.Children.Cast<Canvas>().First(c => c.Name == "EyeClosedIcon");
                var eyeOpenIcon = eyeGrid.Children.Cast<Canvas>().First(c => c.Name == "EyeOpenIcon");

                if (!_isPasswordVisible)
                {
                    passwordTextBox.Text = passwordBox.Password;
                    passwordBox.Visibility = Visibility.Collapsed;
                    passwordTextBox.Visibility = Visibility.Visible;
                    passwordTextBox.Focus();
                    passwordTextBox.CaretIndex = passwordTextBox.Text.Length;

                    eyeClosedIcon.Visibility = Visibility.Collapsed;
                    eyeOpenIcon.Visibility = Visibility.Visible;
                    _isPasswordVisible = true;
                }
                else
                {
                    passwordBox.Password = passwordTextBox.Text;
                    passwordTextBox.Visibility = Visibility.Collapsed;
                    passwordBox.Visibility = Visibility.Visible;
                    passwordBox.Focus();

                    eyeOpenIcon.Visibility = Visibility.Collapsed;
                    eyeClosedIcon.Visibility = Visibility.Visible;
                    _isPasswordVisible = false;
                }
            };

            loginButton.Click += async (s, e) =>
            {
                if (isLoading) return;

                var password = _isPasswordVisible ? passwordTextBox.Text : passwordBox.Password;

                if (string.IsNullOrEmpty(password))
                {
                    ShowLoginError("Введите пароль для входа");
                    return;
                }

                isLoading = true;
                var originalContent = loginButton.Content;
                loginButton.Content = "🔄 Проверка...";
                loginButton.IsEnabled = false;
                cancelButton.IsEnabled = false;

                try
                {
                    // Вход через Supabase с фиксированным email
                    var success = await SupabaseAuthManager.SignInAsync("serp.2001@mail.ru", password);

                    if (success)
                    {
                        // Проверяем права администратора
                        var isAdmin = await SupabasePriceManager.IsCurrentUserAdmin();

                        if (isAdmin)
                        {
                            loginButton.Content = "✅ Успешно!";
                            await Task.Delay(500); // Небольшая задержка для показа успеха
                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            ShowLoginError("У вас нет прав администратора");
                            await SupabaseAuthManager.SignOutAsync();
                        }
                    }
                    else
                    {
                        ShowLoginError("Неверный пароль или email");
                    }
                }
                catch (Exception ex)
                {
                    // Различаем типы ошибок для более точных сообщений
                    string errorMessage;
                    if (ex.Message.Contains("Invalid login credentials") ||
                        ex.Message.Contains("invalid_grant") ||
                        ex.Message.Contains("400"))
                    {
                        errorMessage = "Неверный пароль";
                    }
                    else if (ex.Message.Contains("Network") || ex.Message.Contains("timeout"))
                    {
                        errorMessage = "Ошибка подключения к серверу";
                    }
                    else
                    {
                        errorMessage = "Ошибка входа в систему";
                    }

                    ShowLoginError(errorMessage);
                }
                finally
                {
                    isLoading = false;
                    loginButton.Content = originalContent;
                    loginButton.IsEnabled = true;
                    cancelButton.IsEnabled = true;

                    // Очищаем поле пароля при неудачной авторизации
                    if (!DialogResult.HasValue || !DialogResult.Value)
                    {
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
                if (e.Key == Key.Enter && !isLoading)
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

        // Методы для создания шаблонов со скругленными углами
        private ControlTemplate CreateRoundedPasswordBoxTemplate()
        {
            var template = new ControlTemplate(typeof(PasswordBox));

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            scrollViewer.SetBinding(FrameworkElement.MarginProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });

            border.AppendChild(scrollViewer);
            template.VisualTree = border;

            // Триггеры для фокуса
            var focusTrigger = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
            focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(139, 92, 246))));
            focusTrigger.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2, 2, 2, 2)));

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(139, 92, 246))));

            template.Triggers.Add(focusTrigger);
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        private ControlTemplate CreateRoundedTextBoxTemplate()
        {
            var template = new ControlTemplate(typeof(TextBox));

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            scrollViewer.SetBinding(FrameworkElement.MarginProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });

            border.AppendChild(scrollViewer);
            template.VisualTree = border;

            // Триггеры для фокуса
            var focusTrigger = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
            focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(139, 92, 246))));
            focusTrigger.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2, 2, 2, 2)));

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(139, 92, 246))));

            template.Triggers.Add(focusTrigger);
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        private ControlTemplate CreateRoundedButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            border.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = RelativeSource.TemplatedParent });

            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            // Триггеры для эффектов
            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.9));

            var pressedTrigger = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.8));

            template.Triggers.Add(hoverTrigger);
            template.Triggers.Add(pressedTrigger);

            return template;
        }

        private Border _errorMessageBorder;

        private void ShowLoginError(string message)
        {
            // Получаем ссылку на родительскую сетку
            var parentGrid = (Grid)((Border)Content).Child;

            // Удаляем старое сообщение об ошибке, если есть
            if (_errorMessageBorder != null)
            {
                parentGrid.Children.Remove(_errorMessageBorder);
            }

            // Создаем новое сообщение об ошибке (размещаем внизу под кнопками)
            _errorMessageBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(32, 8, 32, 16),
                Padding = new Thickness(16, 12, 16, 12),
                MaxWidth = 350 // Ограничиваем ширину для лучшего отображения
            };

            var errorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var errorIcon = new TextBlock
            {
                Text = "⚠️",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var errorText = new TextBlock
            {
                Text = message,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 280, // Обеспечиваем видимость всего текста
                LineHeight = 18
            };

            errorPanel.Children.Add(errorIcon);
            errorPanel.Children.Add(errorText);
            _errorMessageBorder.Child = errorPanel;

            // Размещаем ошибку внизу под кнопками (строка 3)
            Grid.SetRow(_errorMessageBorder, 3);
            parentGrid.Children.Add(_errorMessageBorder);

            // Анимация появления
            _errorMessageBorder.Opacity = 0;
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            _errorMessageBorder.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);

            // Автоматически скрываем через 2 секунды
            Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_errorMessageBorder != null && parentGrid.Children.Contains(_errorMessageBorder))
                    {
                        var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                        fadeOutAnimation.Completed += (s, e) =>
                        {
                            parentGrid.Children.Remove(_errorMessageBorder);
                            _errorMessageBorder = null;
                        };
                        _errorMessageBorder.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
                    }
                });
            });
        }
    }
}
