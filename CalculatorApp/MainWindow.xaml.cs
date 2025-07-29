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
using System.Windows.Media.Effects;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Data;

namespace CalculatorApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<CoverageItem> _coverageItems;
        private readonly ObservableCollection<HockeyRinkItem> _hockeyRinkItems;
        private readonly ObservableCollection<USPItem> _uspItems;
        private readonly ObservableCollection<USPRoundItem> _uspRoundItems;
        private ObservableCollection<PriceItem> _priceItems;
        private ObservableCollection<HockeyRinkPriceItem> _hockeyPriceItems;
        private ObservableCollection<USPPriceItem> _uspPriceItems;
        private ObservableCollection<USPRoundPriceItem> _uspRoundPriceItems;
        private CurrentUser _currentUser;

        private bool _isInitialized = false;

        public ObservableCollection<CoverageItem> CoverageItems => _coverageItems;
        public ObservableCollection<HockeyRinkItem> HockeyRinkItems => _hockeyRinkItems;
        public ObservableCollection<USPItem> USPItems => _uspItems;
        public ObservableCollection<USPRoundItem> USPRoundItems => _uspRoundItems;
        public bool HasItems => _coverageItems?.Count > 0;
        public bool HasHockeyItems => _hockeyRinkItems?.Count > 0;
        public bool HasUSPItems => _uspItems?.Count > 0;
        public bool HasUSPRoundItems => _uspRoundItems?.Count > 0;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация коллекций
            _coverageItems = new ObservableCollection<CoverageItem>();
            _hockeyRinkItems = new ObservableCollection<HockeyRinkItem>();
            _uspItems = new ObservableCollection<USPItem>();
            _uspRoundItems = new ObservableCollection<USPRoundItem>();

            // Подписка на изменения коллекций
            _coverageItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasItems));
            };

            _hockeyRinkItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasHockeyItems));
            };

            _uspItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasUSPItems));
            };

            _uspRoundItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasUSPRoundItems));
            };

            PasswordManager.Initialize();
            _currentUser = new CurrentUser();
            DataContext = this;
            _ = InitializeWithLoadingAsync();
        }

        private async Task InitializeWithLoadingAsync()
        {
            try
            {
                // Подключение к облачным сервисам для обычных покрытий
                UpdateLoadingStatus("Загрузка цен на покрытия...");
                UpdateLoadingProgress(20);
                await PriceManager.InitializeWithCloudAsync();
                CompleteLoadingTask("✓ Цены на покрытия загружены");

                // Подключение к облачным сервисам для хоккейных коробок
                UpdateLoadingStatus("Загрузка цен хоккейных коробок...");
                UpdateLoadingProgress(40);
                await HockeyRinkPriceManager.InitializeWithCloudAsync();
                CompleteLoadingTask("✓ Цены хоккейных коробок загружены");

                // Подключение к облачным сервисам для УСП
                UpdateLoadingStatus("Загрузка цен УСП...");
                UpdateLoadingProgress(60);
                await USPPriceManager.InitializeWithCloudAsync();
                CompleteLoadingTask("✓ Цены УСП загружены");

                // Подключение к облачным сервисам для УСП из круглой трубы
                UpdateLoadingStatus("Загрузка цен УСП из круглой трубы...");
                UpdateLoadingProgress(80);
                await USPRoundPriceManager.InitializeWithCloudAsync();
                CompleteLoadingTask("✓ Цены УСП из круглой трубы загружены");

                // Обновление заголовка окна
                UpdateLoadingStatus("Настройка интерфейса...");
                UpdateLoadingProgress(90);
                var priceMode = PriceManager.GetModeString();
                var hockeyMode = HockeyRinkPriceManager.GetModeString();
                var uspMode = USPPriceManager.GetModeString();
                var uspRoundMode = USPRoundPriceManager.GetModeString();

                if (priceMode == hockeyMode && hockeyMode == uspMode && uspMode == uspRoundMode)
                {
                    this.Title = $"Калькулятор покрытий - {priceMode}";
                }
                else
                {
                    this.Title = $"Калькулятор покрытий - Покрытия: {priceMode}, Хоккей: {hockeyMode}, УСП: {uspMode}, УСП круглая труба: {uspRoundMode}";
                }

                // Инициализация расчетов по умолчанию
                AddNewCalculation();
                AddNewHockeyCalculation();
                AddNewUSPCalculation();
                AddNewUSPRoundCalculation();

                // Загрузка данных для админ-панели
                LoadPricesForAdmin();
                LoadHockeyPricesForAdmin();
                LoadUSPPricesForAdmin();
                LoadUSPRoundPricesForAdmin();

                UpdateLoadingProgress(100);
                CompleteLoadingTask("✓ Инициализация завершена");

                // Скрытие экрана загрузки
                CompleteLoading();
            }
            catch (Exception ex)
            {
                UpdateLoadingStatus("Переход в локальный режим");
                CompleteLoadingTask("⚠️ Работа в локальном режиме");

                // Fallback к локальному режиму
                try
                {
                    PriceManager.LoadPrices();
                    HockeyRinkPriceManager.Initialize();
                    USPPriceManager.Initialize();
                    USPRoundPriceManager.Initialize();
                    this.Title = "Калькулятор покрытий - Локальный режим";

                    AddNewCalculation();
                    AddNewHockeyCalculation();
                    AddNewUSPCalculation();
                    AddNewUSPRoundCalculation();

                    LoadPricesForAdmin();
                    LoadHockeyPricesForAdmin();
                    LoadUSPPricesForAdmin();
                    LoadUSPRoundPricesForAdmin();
                }
                catch (Exception fallbackEx)
                {
                    MessageBox.Show($"Критическая ошибка инициализации: {fallbackEx.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                UpdateLoadingProgress(100);
                CompleteLoading();
            }
        }



        private void UpdateLoadingStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                (FindName("LoadingStatusText") as TextBlock)?.SetValue(TextBlock.TextProperty, status);
            });
        }

        private void UpdateLoadingProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                var progressBar = FindName("LoadingProgressBar") as FrameworkElement;
                progressBar?.BeginAnimation(FrameworkElement.WidthProperty,
                    new DoubleAnimation(progressBar.Width, (progress / 100) * 300, TimeSpan.FromMilliseconds(300)));
            });
        }

        private void CompleteLoadingTask(string taskName)
        {
            Dispatcher.Invoke(() =>
            {
                var tasksList = FindName("LoadingTasksList") as Panel;
                if (tasksList == null) return;

                var taskItem = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 240, 253, 244)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 167, 243, 208)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8),
                    Child = new TextBlock
                    {
                        Text = taskName,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),
                        FontWeight = FontWeights.Medium
                    },
                    Opacity = 0
                };

                tasksList.Children.Add(taskItem);
                taskItem.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
            });
        }

        private void CompleteLoading()
        {
            Dispatcher.Invoke(() =>
            {
                var loadingOverlay = FindName("LoadingOverlay") as FrameworkElement;
                if (loadingOverlay != null)
                {
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
                    fadeOut.Completed += (s, e) =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        _isInitialized = true;
                    };
                    loadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
                else
                {
                    _isInitialized = true;
                }
            });
        }

        #region Навигация

        private void NavigateToCalculator(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            SetActivePage("Calculator");
        }

        private void NavigateToHockey(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            SetActivePage("Hockey");
        }

        private void NavigateToUSP(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            SetActivePage("USP");
        }

        private void NavigateToUSPRound(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            SetActivePage("USPRound");
        }

        private async void NavigateToAdmin(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            // Проверяем пароль
            if (!ValidateAdminPassword())
                return;

            // Обновляем информацию о пользователе после успешной авторизации
            await UpdateCurrentUserInfo();

            LoadPricesForAdmin();
            LoadHockeyPricesForAdmin();
            LoadUSPPricesForAdmin();
            SetActivePage("Admin");
        }

        private async Task UpdateCurrentUserInfo()
        {
            try
            {
                var email = await SupabaseAuthManager.GetCurrentUserEmailAsync();
                if (!string.IsNullOrEmpty(email))
                {
                    _currentUser.Email = email;
                    _currentUser.Id = "supabase_user"; // Можно получить реальный ID из Supabase если нужно
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки используем значения по умолчанию
                _currentUser.Email = "Не авторизован";
                _currentUser.Id = "N/A";
            }
        }

        private void SetActivePage(string pageName)
        {
            // Скрываем все страницы
            var calculatorPage = FindName("CalculatorPage") as FrameworkElement;
            var hockeyPage = FindName("HockeyPage") as FrameworkElement;
            var uspPage = FindName("USPPage") as FrameworkElement;
            var uspRoundPage = FindName("USPRoundPage") as FrameworkElement;
            var adminPage = FindName("AdminPage") as FrameworkElement;

            calculatorPage?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            hockeyPage?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            uspPage?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            uspRoundPage?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            adminPage?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

            // Сбрасываем активные кнопки
            var calculatorNavButton = FindName("CalculatorNavButton") as FrameworkElement;
            var hockeyNavButton = FindName("HockeyNavButton") as FrameworkElement;
            var uspNavButton = FindName("USPNavButton") as FrameworkElement;
            var uspRoundNavButton = FindName("USPRoundNavButton") as FrameworkElement;
            var adminNavButton = FindName("AdminNavButton") as FrameworkElement;

            calculatorNavButton?.SetValue(FrameworkElement.TagProperty, null);
            hockeyNavButton?.SetValue(FrameworkElement.TagProperty, null);
            uspNavButton?.SetValue(FrameworkElement.TagProperty, null);
            uspRoundNavButton?.SetValue(FrameworkElement.TagProperty, null);
            adminNavButton?.SetValue(FrameworkElement.TagProperty, null);

            // Показываем нужную страницу и активируем кнопку
            switch (pageName)
            {
                case "Calculator":
                    calculatorPage?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    calculatorNavButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "Hockey":
                    hockeyPage?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    hockeyNavButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "USP":
                    uspPage?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    uspNavButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "USPRound":
                    uspRoundPage?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    uspRoundNavButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "Admin":
                    adminPage?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    adminNavButton?.SetValue(FrameworkElement.TagProperty, "Active");
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
            if (sender is Button button && button.Tag is CoverageItem item)
            {
                var text = item.HasError ? item.ErrorMessage : $"{item.FinalCost:F0}";
                CopyToClipboard(text, button);
            }
        }

        private async void CopyToClipboard(string text, Button sourceButton = null)
        {
            Clipboard.SetText(text);
            await ShowNotification("✓ Скопировано в буфер обмена!");
        }

        private async Task ShowNotification(string message)
        {
            var notification = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 20, 0, 0),
                Opacity = 0,
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };

            var mainGrid = (Grid)this.Content;
            if (mainGrid == null) return;

            mainGrid.Children.Add(notification);
            Grid.SetColumnSpan(notification, 2);

            notification.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
            await Task.Delay(2500);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => mainGrid.Children.Remove(notification);
            notification.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        #endregion

        #region Калькулятор хоккейных коробок

        private void AddNewHockeyCalculation()
        {
            var newItem = new HockeyRinkItem();
            _hockeyRinkItems.Add(newItem);
        }

        private void AddHockeyCalculation_Click(object sender, RoutedEventArgs e)
        {
            AddNewHockeyCalculation();
        }

        private void ClearAllHockey_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить все расчеты хоккейных коробок?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _hockeyRinkItems.Clear();
                AddNewHockeyCalculation(); // Добавляем один пустой расчет
            }
        }

        private void DeleteHockeyCalculation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is HockeyRinkItem item)
            {
                // Если это единственный расчет, не удаляем
                if (_hockeyRinkItems.Count <= 1)
                {
                    MessageBox.Show("Нельзя удалить единственный расчет. Добавьте новый расчет перед удалением текущего.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("Удалить этот расчет?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _hockeyRinkItems.Remove(item);
                }
            }
        }

        private void CopyHockeyResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string text = "";
                if (button.Tag is HockeyCalculationResult result)
                {
                    text = result.FormattedText;
                }
                else if (button.Tag is HockeyRinkItem item)
                {
                    var header = $"Хоккейная коробка {item.Width}x{item.Length}м (R={item.Radius}м)\nСтекло: {item.GlassThickness}, Сетка: {item.NetHeight}\n\n";
                    text = header + (item.Calculations?.Count > 0
                        ? string.Join("\n\n", item.Calculations.Select(calc => calc.FormattedText))
                        : "Результаты расчета недоступны");
                }
                CopyToClipboard(text, button);
            }
        }

        private void CopyPriceToClipboard(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(sender is TextBox textBox)) return;

                string textToCopy = FormatPriceValue(textBox.Tag) ?? ExtractTextValue(textBox.Text);

                if (!string.IsNullOrEmpty(textToCopy))
                {
                    Clipboard.SetText(textToCopy);
                    ShowPriceCopyNotification(textBox, textToCopy);
                }
                e.Handled = true;
            }
            catch
            {
                if (sender is TextBox tb)
                    ShowPriceCopyNotification(tb, "Ошибка копирования");
                e.Handled = true;
            }
        }

        private string FormatPriceValue(object value)
        {
            if (value == null) return null;

            if (double.TryParse(value.ToString(), out var numericValue))
            {
                if (numericValue >= 1000)
                    return Math.Ceiling(numericValue).ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
                else if (numericValue != Math.Floor(numericValue))
                    return numericValue < 100 ? numericValue.ToString("F2", CultureInfo.InvariantCulture)
                                               : numericValue.ToString("F1", CultureInfo.InvariantCulture);
                else
                    return numericValue.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
            }
            return value.ToString();
        }

        private string ExtractTextValue(string text)
        {
            var cleaned = text.Replace(" ₽", "").Replace("₽", "").Replace(" кг", "").Replace(" м³", "")
                             .Replace("Секций: ", "").Replace("Сеток: ", "").Replace("Масса: ", "").Replace("Объем: ", "").Trim();

            if (double.TryParse(cleaned.Replace(" ", "").Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue >= 1000 ? Math.Ceiling(parsedValue).ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ")
                                           : Math.Ceiling(parsedValue).ToString("F0", CultureInfo.InvariantCulture);
            }
            return cleaned.Contains(" ") && !cleaned.Contains(".") && !cleaned.Contains(",") ? cleaned : cleaned.Replace(" ", "");
        }

        private async void ShowPriceCopyNotification(TextBox textBox, string copiedText)
        {
            try
            {
                // Создаем компактное уведомление в центре экрана
                var notification = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 10, 16, 10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 50, 0, 0), // Увеличил отступ сверху
                    Opacity = 0,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.15,
                        BlurRadius = 12,
                        ShadowDepth = 4
                    }
                };

                var textBlock = new TextBlock
                {
                    Text = $"📋 Скопировано: {copiedText}",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                notification.Child = textBlock;

                // Ищем главную сетку для добавления уведомления
                var mainGrid = this.Content as Grid;
                if (mainGrid == null)
                {
                    // Fallback - если не можем найти главную сетку, используем MessageBox
                    MessageBox.Show($"📋 Скопировано: {copiedText}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Добавляем уведомление поверх всех элементов
                mainGrid.Children.Add(notification);
                Grid.SetRowSpan(notification, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 2);
                Grid.SetColumnSpan(notification, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 2);
                Panel.SetZIndex(notification, 1000); // Высокий Z-index чтобы показать поверх всего

                // Анимация появления
                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
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
                    try
                    {
                        if (mainGrid.Children.Contains(notification))
                        {
                            mainGrid.Children.Remove(notification);
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки при удалении
                    }
                };

                notification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            }
            catch (Exception ex)
            {
                // Fallback - если анимация не работает, используем обычный MessageBox
                MessageBox.Show($"📋 Скопировано: {copiedText}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Калькулятор УСП из круглой трубы

        private void AddNewUSPRoundCalculation()
        {
            var newItem = new USPRoundItem();
            _uspRoundItems.Add(newItem);
        }

        private void AddUSPRoundCalculation_Click(object sender, RoutedEventArgs e)
        {
            AddNewUSPRoundCalculation();
        }

        private void ClearAllUSPRound_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить все расчеты УСП из круглой трубы?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _uspRoundItems.Clear();
                AddNewUSPRoundCalculation();
            }
        }

        private void DeleteUSPRoundCalculation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is USPRoundItem item)
            {
                if (_uspRoundItems.Count <= 1)
                {
                    MessageBox.Show("Нельзя удалить единственный расчет. Добавьте новый расчет перед удалением текущего.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("Удалить этот расчет УСП из круглой трубы?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _uspRoundItems.Remove(item);
                }
            }
        }

        private void CopyUSPRoundResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string text = "";
                if (button.Tag is USPRoundCalculationResult result)
                {
                    text = result.FormattedText;
                }
                else if (button.Tag is USPRoundItem item)
                {
                    var header = $"УСП из круглой трубы {item.Length}x{item.Width}м (периметр {item.Perimeter}м.п.)\n\n";
                    text = header + (item.Calculations?.Count > 0
                        ? string.Join("\n\n", item.Calculations.Select(calc => calc.FormattedText))
                        : "Результаты расчета недоступны");
                }
                CopyToClipboard(text, button);
            }
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
            var currentPasswordTextBox = FindName("CurrentPasswordTextBox") as TextBox;
            var currentPasswordBox = FindName("CurrentPasswordBox") as PasswordBox;
            var currentEyeOpenIcon = FindName("CurrentEyeOpenIcon") as FrameworkElement;
            var currentEyeClosedIcon = FindName("CurrentEyeClosedIcon") as FrameworkElement;

            if (currentPasswordTextBox == null || currentPasswordBox == null ||
                currentEyeOpenIcon == null || currentEyeClosedIcon == null)
                return;

            var isVisible = currentPasswordTextBox.Visibility == Visibility.Visible;

            if (isVisible)
            {
                currentPasswordBox.Password = currentPasswordTextBox.Text;
                currentPasswordTextBox.Visibility = Visibility.Collapsed;
                currentPasswordBox.Visibility = Visibility.Visible;
                currentPasswordBox.Focus();

                currentEyeOpenIcon.Visibility = Visibility.Collapsed;
                currentEyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                currentPasswordTextBox.Text = currentPasswordBox.Password;
                currentPasswordBox.Visibility = Visibility.Collapsed;
                currentPasswordTextBox.Visibility = Visibility.Visible;
                currentPasswordTextBox.Focus();
                currentPasswordTextBox.CaretIndex = currentPasswordTextBox.Text.Length;

                currentEyeClosedIcon.Visibility = Visibility.Collapsed;
                currentEyeOpenIcon.Visibility = Visibility.Visible;
            }
        }

        private void ToggleNewPasswordVisibility(object sender, RoutedEventArgs e)
        {
            var newPasswordTextBox = FindName("NewPasswordTextBox") as TextBox;
            var newPasswordBox = FindName("NewPasswordBox") as PasswordBox;
            var newEyeOpenIcon = FindName("NewEyeOpenIcon") as FrameworkElement;
            var newEyeClosedIcon = FindName("NewEyeClosedIcon") as FrameworkElement;

            if (newPasswordTextBox == null || newPasswordBox == null ||
                newEyeOpenIcon == null || newEyeClosedIcon == null)
                return;

            var isVisible = newPasswordTextBox.Visibility == Visibility.Visible;

            if (isVisible)
            {
                newPasswordBox.Password = newPasswordTextBox.Text;
                newPasswordTextBox.Visibility = Visibility.Collapsed;
                newPasswordBox.Visibility = Visibility.Visible;
                newPasswordBox.Focus();

                newEyeOpenIcon.Visibility = Visibility.Collapsed;
                newEyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                newPasswordTextBox.Text = newPasswordBox.Password;
                newPasswordBox.Visibility = Visibility.Collapsed;
                newPasswordTextBox.Visibility = Visibility.Visible;
                newPasswordTextBox.Focus();
                newPasswordTextBox.CaretIndex = newPasswordTextBox.Text.Length;

                newEyeClosedIcon.Visibility = Visibility.Collapsed;
                newEyeOpenIcon.Visibility = Visibility.Visible;
            }
        }

        private void ToggleConfirmPasswordVisibility(object sender, RoutedEventArgs e)
        {
            var confirmPasswordTextBox = FindName("ConfirmPasswordTextBox") as TextBox;
            var confirmPasswordBox = FindName("ConfirmPasswordBox") as PasswordBox;
            var confirmEyeOpenIcon = FindName("ConfirmEyeOpenIcon") as FrameworkElement;
            var confirmEyeClosedIcon = FindName("ConfirmEyeClosedIcon") as FrameworkElement;

            if (confirmPasswordTextBox == null || confirmPasswordBox == null ||
                confirmEyeOpenIcon == null || confirmEyeClosedIcon == null)
                return;

            var isVisible = confirmPasswordTextBox.Visibility == Visibility.Visible;

            if (isVisible)
            {
                confirmPasswordBox.Password = confirmPasswordTextBox.Text;
                confirmPasswordTextBox.Visibility = Visibility.Collapsed;
                confirmPasswordBox.Visibility = Visibility.Visible;
                confirmPasswordBox.Focus();

                confirmEyeOpenIcon.Visibility = Visibility.Collapsed;
                confirmEyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                confirmPasswordTextBox.Text = confirmPasswordBox.Password;
                confirmPasswordBox.Visibility = Visibility.Collapsed;
                confirmPasswordTextBox.Visibility = Visibility.Visible;
                confirmPasswordTextBox.Focus();
                confirmPasswordTextBox.CaretIndex = confirmPasswordTextBox.Text.Length;

                confirmEyeClosedIcon.Visibility = Visibility.Collapsed;
                confirmEyeOpenIcon.Visibility = Visibility.Visible;
            }
        }

        // Метод смены пароля (работает с паролем Supabase)
        private async void ChangePasswordClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var errorMessage = FindName("ErrorMessage") as FrameworkElement;
                errorMessage?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

                var currentPasswordTextBox = FindName("CurrentPasswordTextBox") as TextBox;
                var currentPasswordBox = FindName("CurrentPasswordBox") as PasswordBox;
                var newPasswordTextBox = FindName("NewPasswordTextBox") as TextBox;
                var newPasswordBox = FindName("NewPasswordBox") as PasswordBox;
                var confirmPasswordTextBox = FindName("ConfirmPasswordTextBox") as TextBox;
                var confirmPasswordBox = FindName("ConfirmPasswordBox") as PasswordBox;

                if (currentPasswordTextBox == null || currentPasswordBox == null ||
                    newPasswordTextBox == null || newPasswordBox == null ||
                    confirmPasswordTextBox == null || confirmPasswordBox == null)
                    return;

                var currentPassword = currentPasswordTextBox.Visibility == Visibility.Visible
                    ? currentPasswordTextBox.Text
                    : currentPasswordBox.Password;

                var newPassword = newPasswordTextBox.Visibility == Visibility.Visible
                    ? newPasswordTextBox.Text
                    : newPasswordBox.Password;

                var confirmPassword = confirmPasswordTextBox.Visibility == Visibility.Visible
                    ? confirmPasswordTextBox.Text
                    : confirmPasswordBox.Password;

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
            var errorMessageText = FindName("ErrorMessageText") as TextBlock;
            var errorMessage = FindName("ErrorMessage") as FrameworkElement;

            if (errorMessageText != null)
                errorMessageText.Text = message;
            if (errorMessage != null)
                errorMessage.Visibility = Visibility.Visible;
        }

        private async void ShowCurrentPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentEmail = await SupabaseAuthManager.GetCurrentUserEmailAsync();
                var message = $"Текущий пользователь: {currentEmail}\n\n" +
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
            var currentPasswordBox = FindName("CurrentPasswordBox") as PasswordBox;
            var currentPasswordTextBox = FindName("CurrentPasswordTextBox") as TextBox;
            var newPasswordBox = FindName("NewPasswordBox") as PasswordBox;
            var newPasswordTextBox = FindName("NewPasswordTextBox") as TextBox;
            var confirmPasswordBox = FindName("ConfirmPasswordBox") as PasswordBox;
            var confirmPasswordTextBox = FindName("ConfirmPasswordTextBox") as TextBox;

            currentPasswordBox?.Clear();
            currentPasswordTextBox?.Clear();
            newPasswordBox?.Clear();
            newPasswordTextBox?.Clear();
            confirmPasswordBox?.Clear();
            confirmPasswordTextBox?.Clear();
        }



        private void LoadPricesForAdmin()
        {
            var prices = PriceManager.GetAllPrices();
            _priceItems = new ObservableCollection<PriceItem>(prices);

            var priceItemsControl = FindName("PriceItemsControl") as ItemsControl;
            if (priceItemsControl != null)
                priceItemsControl.ItemsSource = _priceItems;
        }

        private void LoadHockeyPricesForAdmin()
        {
            var hockeyPrices = HockeyRinkPriceManager.GetAllPrices();
            _hockeyPriceItems = new ObservableCollection<HockeyRinkPriceItem>(hockeyPrices);

            var hockeyPriceItemsControl = FindName("HockeyPriceItemsControl") as ItemsControl;
            if (hockeyPriceItemsControl != null)
                hockeyPriceItemsControl.ItemsSource = _hockeyPriceItems;

            // Загружаем коэффициенты
            var coefficients = HockeyRinkPriceManager.GetCoefficients();
            var dealerCoeffTextBox = FindName("DealerCoeffTextBox") as TextBox;
            var wholesaleCoeffTextBox = FindName("WholesaleCoeffTextBox") as TextBox;
            var estimateCoeffTextBox = FindName("EstimateCoeffTextBox") as TextBox;

            if (dealerCoeffTextBox != null)
                dealerCoeffTextBox.Text = coefficients.DealerCoeff.ToString();
            if (wholesaleCoeffTextBox != null)
                wholesaleCoeffTextBox.Text = coefficients.WholesaleCoeff.ToString();
            if (estimateCoeffTextBox != null)
                estimateCoeffTextBox.Text = coefficients.EstimateCoeff.ToString();
        }

        private async void SavePrices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем валидность данных
                if (_priceItems.Any(p => p.Price <= 0 || string.IsNullOrWhiteSpace(p.Type) || string.IsNullOrWhiteSpace(p.Thickness)))
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

                        foreach (var item in _hockeyRinkItems)
                        {
                            item.RefreshPrices();
                        }

                        foreach (var item in _uspItems)
                        {
                            item.RefreshPrices();
                        }

                        foreach (var item in _uspRoundItems)
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

        private async void SaveHockeyPrices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем валидность данных
                if (_hockeyPriceItems.Any(p => p.Price <= 0 || string.IsNullOrWhiteSpace(p.Category) || string.IsNullOrWhiteSpace(p.Subcategory)))
                {
                    MessageBox.Show("Обнаружены некорректные данные. Проверьте, что все цены больше 0 и все поля заполнены.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Показываем индикатор загрузки
                var loadingWindow = ShowLoadingWindow("Сохранение данных хоккейных коробок...");

                try
                {
                    // Получаем коэффициенты из полей
                    var coefficients = new HockeyRinkCoefficients
                    {
                        DealerCoeff = double.TryParse(DealerCoeffTextBox.Text, out var dealer) ? dealer : 1.25,
                        WholesaleCoeff = double.TryParse(WholesaleCoeffTextBox.Text, out var wholesale) ? wholesale : 1.20,
                        EstimateCoeff = double.TryParse(EstimateCoeffTextBox.Text, out var estimate) ? estimate : 1.80
                    };

                    // Сохраняем цены и коэффициенты (поддерживает облако)
                    var success = await HockeyRinkPriceManager.SavePricesAsync(_hockeyPriceItems.ToList(), coefficients);

                    loadingWindow.Close();

                    if (success)
                    {
                        var mode = HockeyRinkPriceManager.IsOnlineMode() ? "в облаке и локально" : "локально";
                        MessageBox.Show($"Цены хоккейных коробок успешно сохранены {mode}!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем существующие расчеты
                        foreach (var item in _hockeyRinkItems)
                        {
                            item.RecalculateAll();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка сохранения данных хоккейных коробок", "Ошибка",
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
                MessageBox.Show($"Ошибка при сохранении цен хоккейных коробок: {ex.Message}", "Ошибка",
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

        private void ResetHockeyPrices_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите сбросить все цены хоккейных коробок к значениям по умолчанию? Это действие нельзя отменить.",
                "Подтверждение сброса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    HockeyRinkPriceManager.ResetToDefaults();
                    LoadHockeyPricesForAdmin();

                    // Обновляем поля коэффициентов
                    var coefficients = HockeyRinkPriceManager.GetCoefficients();
                    DealerCoeffTextBox.Text = coefficients.DealerCoeff.ToString();
                    WholesaleCoeffTextBox.Text = coefficients.WholesaleCoeff.ToString();
                    EstimateCoeffTextBox.Text = coefficients.EstimateCoeff.ToString();

                    MessageBox.Show("Цены хоккейных коробок сброшены к значениям по умолчанию.", "Сброс выполнен",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сбросе цен хоккейных коробок: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Калькулятор УСП

        private void AddNewUSPCalculation()
        {
            var newItem = new USPItem();
            _uspItems.Add(newItem);
        }

        private void AddUSPCalculation_Click(object sender, RoutedEventArgs e)
        {
            AddNewUSPCalculation();
        }

        private void ClearAllUSP_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить все расчеты УСП?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _uspItems.Clear();
                AddNewUSPCalculation(); // Добавляем один пустой расчет
            }
        }

        private void DeleteUSPCalculation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is USPItem item)
            {
                // Если это единственный расчет, не удаляем
                if (_uspItems.Count <= 1)
                {
                    MessageBox.Show("Нельзя удалить единственный расчет. Добавьте новый расчет перед удалением текущего.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("Удалить этот расчет УСП?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _uspItems.Remove(item);
                }
            }
        }

        private void CopyUSPResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string text = "";
                if (button.Tag is USPCalculationResult result)
                {
                    text = result.FormattedText;
                }
                else if (button.Tag is USPItem item)
                {
                    var header = $"УСП площадка {item.Length}x{item.Width}м (периметр {item.Perimeter}м.п.)\nВысота: {item.Height}, Столбы: {item.ColumnType}\n\n";
                    text = header + (item.Calculations?.Count > 0
                        ? string.Join("\n\n", item.Calculations.Select(calc => calc.FormattedText))
                        : "Результаты расчета недоступны");
                }
                CopyToClipboard(text, button);
            }
        }

        #endregion

        #region Информация о системе

        private void LoadUSPPricesForAdmin()
        {
            var uspPrices = USPPriceManager.GetAllPrices();
            _uspPriceItems = new ObservableCollection<USPPriceItem>(uspPrices);
            USPPriceItemsControl.ItemsSource = _uspPriceItems;

            // Загружаем коэффициенты
            var coefficients = USPPriceManager.GetCoefficients();
            USPWholesaleCoeffTextBox.Text = coefficients.WholesaleCoeff.ToString();
            USPDealerCoeffTextBox.Text = coefficients.DealerCoeff.ToString();
        }

        private void LoadUSPRoundPricesForAdmin()
        {
            var uspRoundPrices = USPRoundPriceManager.GetAllPrices();
            _uspRoundPriceItems = new ObservableCollection<USPRoundPriceItem>(uspRoundPrices);
            USPRoundPriceItemsControl.ItemsSource = _uspRoundPriceItems;

            // Загружаем коэффициенты
            var coefficients = USPRoundPriceManager.GetCoefficients();
            USPRoundWholesaleCoeffTextBox.Text = coefficients.WholesaleCoeff.ToString();
            USPRoundDealerCoeffTextBox.Text = coefficients.DealerCoeff.ToString();
            USPRoundPowderPriceTextBox.Text = coefficients.PowderPricePerM2.ToString();
            USPRoundPaintingCoeffTextBox.Text = coefficients.PaintingCoeff.ToString();
            USPRoundPaintingSecondCoeffTextBox.Text = coefficients.PaintingSecondCoeff.ToString();

            // Загружаем фиксированные значения
            var fixedValues = USPRoundPriceManager.GetFixedValues();
            BasketballStandMassTextBox.Text = fixedValues.BasketballStand.Mass.ToString();
            BasketballStandVolumeTextBox.Text = fixedValues.BasketballStand.Volume.ToString();
            Gate3mMassTextBox.Text = fixedValues.Gate3m.Mass.ToString();
            Gate3mVolumeTextBox.Text = fixedValues.Gate3m.Volume.ToString();
            Gate41mMassTextBox.Text = fixedValues.Gate41m.Mass.ToString();
            Gate41mVolumeTextBox.Text = fixedValues.Gate41m.Volume.ToString();
        }

        private async void SaveUSPPrices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_uspPriceItems.Any(p => p.Price <= 0 || string.IsNullOrWhiteSpace(p.Category) || string.IsNullOrWhiteSpace(p.Subcategory)))
                {
                    MessageBox.Show("Обнаружены некорректные данные. Проверьте, что все цены больше 0 и все поля заполнены.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Показываем индикатор загрузки
                var loadingWindow = ShowLoadingWindow("Сохранение данных УСП...");

                try
                {
                    // Получаем коэффициенты из полей
                    var coefficients = new USPCoefficients
                    {
                        WholesaleCoeff = double.TryParse(USPWholesaleCoeffTextBox.Text, out var wholesale) ? wholesale : 1.8,
                        DealerCoeff = double.TryParse(USPDealerCoeffTextBox.Text, out var dealer) ? dealer : 1.3
                    };

                    // Сохраняем цены и коэффициенты (поддерживает облако)
                    var success = await USPPriceManager.SavePricesAsync(_uspPriceItems.ToList(), coefficients);

                    loadingWindow.Close();

                    if (success)
                    {
                        var mode = USPPriceManager.IsOnlineMode() ? "в облаке и локально" : "локально";
                        MessageBox.Show($"Цены УСП успешно сохранены {mode}!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем существующие расчеты
                        foreach (var item in _uspItems)
                        {
                            item.RecalculateAll();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка сохранения данных УСП", "Ошибка",
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
                MessageBox.Show($"Ошибка при сохранении цен УСП: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetUSPPrices_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите сбросить все цены УСП к значениям по умолчанию? Это действие нельзя отменить.",
                "Подтверждение сброса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    USPPriceManager.ResetToDefaults();
                    LoadUSPPricesForAdmin();

                    // Обновляем поля коэффициентов
                    var coefficients = USPPriceManager.GetCoefficients();
                    USPWholesaleCoeffTextBox.Text = coefficients.WholesaleCoeff.ToString();
                    USPDealerCoeffTextBox.Text = coefficients.DealerCoeff.ToString();

                    MessageBox.Show("Цены УСП сброшены к значениям по умолчанию.", "Сброс выполнен",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сбросе цен УСП: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SaveUSPRoundPrices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_uspRoundPriceItems.Any(p => p.Price <= 0 || string.IsNullOrWhiteSpace(p.Category) || string.IsNullOrWhiteSpace(p.Subcategory)))
                {
                    MessageBox.Show("Обнаружены некорректные данные. Проверьте, что все цены больше 0 и все поля заполнены.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Показываем индикатор загрузки
                var loadingWindow = ShowLoadingWindow("Сохранение данных УСП из круглой трубы...");

                try
                {
                    // Получаем коэффициенты из полей
                    var coefficients = new USPRoundCoefficients
                    {
                        WholesaleCoeff = double.TryParse(USPRoundWholesaleCoeffTextBox.Text, out var wholesale) ? wholesale : 1.202,
                        DealerCoeff = double.TryParse(USPRoundDealerCoeffTextBox.Text, out var dealer) ? dealer : 1.149,
                        PowderPricePerM2 = double.TryParse(USPRoundPowderPriceTextBox.Text, out var powder) ? powder : 150,
                        PaintingCoeff = double.TryParse(USPRoundPaintingCoeffTextBox.Text, out var painting) ? painting : 1.3,
                        PaintingSecondCoeff = double.TryParse(USPRoundPaintingSecondCoeffTextBox.Text, out var paintingSecond) ? paintingSecond : 2.3
                    };

                    // Получаем фиксированные значения из полей
                    var fixedValues = new USPRoundFixedValues
                    {
                        BasketballStand = new USPRoundFixedValues.ElementValues
                        {
                            Mass = double.TryParse(BasketballStandMassTextBox.Text, out var bsMass) ? bsMass : 130,
                            Volume = double.TryParse(BasketballStandVolumeTextBox.Text, out var bsVolume) ? bsVolume : 1.5
                        },
                        Gate3m = new USPRoundFixedValues.ElementValues
                        {
                            Mass = double.TryParse(Gate3mMassTextBox.Text, out var g3Mass) ? g3Mass : 81,
                            Volume = double.TryParse(Gate3mVolumeTextBox.Text, out var g3Volume) ? g3Volume : 0.2
                        },
                        Gate41m = new USPRoundFixedValues.ElementValues
                        {
                            Mass = double.TryParse(Gate41mMassTextBox.Text, out var g41Mass) ? g41Mass : 95,
                            Volume = double.TryParse(Gate41mVolumeTextBox.Text, out var g41Volume) ? g41Volume : 0.25
                        }
                    };

                    // Сохраняем цены и коэффициенты (поддерживает облако)
                    var success = await USPRoundPriceManager.SavePricesAsync(_uspRoundPriceItems.ToList(), coefficients, fixedValues);

                    loadingWindow.Close();

                    if (success)
                    {
                        var mode = USPRoundPriceManager.IsOnlineMode() ? "в облаке и локально" : "локально";
                        MessageBox.Show($"Цены УСП из круглой трубы успешно сохранены {mode}!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем существующие расчеты
                        foreach (var item in _uspRoundItems)
                        {
                            item.RecalculateAll();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка сохранения данных УСП из круглой трубы", "Ошибка",
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
                MessageBox.Show($"Ошибка при сохранении цен УСП из круглой трубы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetUSPRoundPrices_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите сбросить все цены УСП из круглой трубы к значениям по умолчанию? Это действие нельзя отменить.",
                "Подтверждение сброса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    USPRoundPriceManager.ResetToDefaults();
                    LoadUSPRoundPricesForAdmin();

                    MessageBox.Show("Цены УСП из круглой трубы сброшены к значениям по умолчанию.", "Сброс выполнен",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сбросе цен УСП из круглой трубы: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            var priceMode = PriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
            var hockeyMode = HockeyRinkPriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
            var uspMode = USPPriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
            var uspRoundMode = USPRoundPriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";

            var info = $"Режим работы покрытий: {priceMode}\n" +
                      $"Режим работы хоккейных коробок: {hockeyMode}\n" +
                      $"Режим работы УСП: {uspMode}\n" +
                      $"Режим работы УСП из круглой трубы: {uspRoundMode}\n\n" +
                      $"Версия данных покрытий: {SupabasePriceManager.GetCurrentVersion()}\n" +
                      $"Версия данных хоккейных цен: {SupabaseHockeyPriceManager.GetCurrentHockeyVersion()}\n" +
                      $"Версия коэффициентов хоккея: {SupabaseHockeyPriceManager.GetCurrentCoefficientsVersion()}\n" +
                      $"Версия данных УСП: {SupabaseUSPPriceManager.GetCurrentUSPVersion()}\n" +
                      $"Версия коэффициентов УСП: {SupabaseUSPPriceManager.GetCurrentUSPCoefficientsVersion()}\n" +
                      $"Версия данных УСП из круглой трубы: {SupabaseUSPRoundPriceManager.GetCurrentUSPRoundVersion()}\n" +
                      $"Версия коэффициентов УСП из круглой трубы: {SupabaseUSPRoundPriceManager.GetCurrentUSPRoundCoefficientsVersion()}\n" +
                      $"Версия фиксированных значений УСП из круглой трубы: {SupabaseUSPRoundPriceManager.GetCurrentUSPRoundFixedValuesVersion()}";

            MessageBox.Show(info, "Информация о системе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Методы для переключения вкладок администрирования
        private void ShowCoverageTab(object sender, RoutedEventArgs e)
        {
            SwitchAdminTab("coverage");
        }

        private void ShowHockeyTab(object sender, RoutedEventArgs e)
        {
            SwitchAdminTab("hockey");
        }

        private void ShowUSPTab(object sender, RoutedEventArgs e)
        {
            SwitchAdminTab("usp");
        }

        private void ShowUSPRoundTab(object sender, RoutedEventArgs e)
        {
            SwitchAdminTab("uspround");
        }

        private void ShowPasswordTab(object sender, RoutedEventArgs e)
        {
            SwitchAdminTab("password");
        }

        private void SwitchAdminTab(string tabName)
        {
            // Скрываем все вкладки
            var coverageTabContent = FindName("CoverageTabContent") as FrameworkElement;
            var hockeyTabContent = FindName("HockeyTabContent") as FrameworkElement;
            var uspTabContent = FindName("USPTabContent") as FrameworkElement;
            var uspRoundTabContent = FindName("USPRoundTabContent") as FrameworkElement;
            var passwordTabContent = FindName("PasswordTabContent") as FrameworkElement;

            coverageTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            hockeyTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            uspTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            uspRoundTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            passwordTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

            // Сбрасываем стили всех кнопок
            var coverageTabButton = FindName("CoverageTabButton") as FrameworkElement;
            var hockeyTabButton = FindName("HockeyTabButton") as FrameworkElement;
            var uspTabButton = FindName("USPTabButton") as FrameworkElement;
            var uspRoundTabButton = FindName("USPRoundTabButton") as FrameworkElement;
            var passwordTabButton = FindName("PasswordTabButton") as FrameworkElement;

            coverageTabButton?.SetValue(FrameworkElement.TagProperty, null);
            hockeyTabButton?.SetValue(FrameworkElement.TagProperty, null);
            uspTabButton?.SetValue(FrameworkElement.TagProperty, null);
            uspRoundTabButton?.SetValue(FrameworkElement.TagProperty, null);
            passwordTabButton?.SetValue(FrameworkElement.TagProperty, null);

            // Показываем выбранную вкладку и активируем соответствующую кнопку
            switch (tabName)
            {
                case "coverage":
                    coverageTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    coverageTabButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "hockey":
                    hockeyTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    hockeyTabButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "usp":
                    uspTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    uspTabButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "uspround":
                    uspRoundTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    uspRoundTabButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
                case "password":
                    passwordTabContent?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    passwordTabButton?.SetValue(FrameworkElement.TagProperty, "Active");
                    break;
            }
        }
    }

    #region Модели данных и менеджеры

    // Класс для текущего пользователя
    public class CurrentUser
    {
        public string Id { get; set; }
        public string Email { get; set; }
    }

    // Структура для коэффициентов УСП из круглой трубы
    public class USPRoundCoefficients
    {
        public double WholesaleCoeff { get; set; } = 1.202;
        public double DealerCoeff { get; set; } = 1.149;
        public double PowderPricePerM2 { get; set; } = 150;
        public double PaintingCoeff { get; set; } = 1.3;
        public double PaintingSecondCoeff { get; set; } = 2.3;
    }

    // Структура для фиксированных значений УСП из круглой трубы
    public class USPRoundFixedValues
    {
        public class ElementValues
        {
            public double Mass { get; set; }
            public double Volume { get; set; }
        }

        public ElementValues BasketballStand { get; set; } = new ElementValues { Mass = 130, Volume = 1.5 };
        public ElementValues Gate3m { get; set; } = new ElementValues { Mass = 81, Volume = 0.2 };
        public ElementValues Gate41m { get; set; } = new ElementValues { Mass = 95, Volume = 0.25 };
    }

    // Новый класс для одного типа расчета УСП из круглой трубы
    public class USPRoundCalculationResult : INotifyPropertyChanged
    {
        private string _calculationType;
        private double _wholesalePriceZinc;
        private double _dealerPriceZinc;
        private double _factoryCostZinc;
        private double _factoryCostZincPowder;
        private double _wholesalePriceZincPowder;
        private double _dealerPriceZincPowder;
        private double _areaWithColumns;
        private double _mass;
        private double _volume;

        public string CalculationType
        {
            get => _calculationType;
            set
            {
                _calculationType = value;
                OnPropertyChanged(nameof(CalculationType));
            }
        }

        public double WholesalePriceZinc
        {
            get => _wholesalePriceZinc;
            set
            {
                _wholesalePriceZinc = value;
                OnPropertyChanged(nameof(WholesalePriceZinc));
            }
        }

        public double DealerPriceZinc
        {
            get => _dealerPriceZinc;
            set
            {
                _dealerPriceZinc = value;
                OnPropertyChanged(nameof(DealerPriceZinc));
            }
        }

        public double FactoryCostZinc
        {
            get => _factoryCostZinc;
            set
            {
                _factoryCostZinc = value;
                OnPropertyChanged(nameof(FactoryCostZinc));
            }
        }

        public double FactoryCostZincPowder
        {
            get => _factoryCostZincPowder;
            set
            {
                _factoryCostZincPowder = value;
                OnPropertyChanged(nameof(FactoryCostZincPowder));
            }
        }

        public double WholesalePriceZincPowder
        {
            get => _wholesalePriceZincPowder;
            set
            {
                _wholesalePriceZincPowder = value;
                OnPropertyChanged(nameof(WholesalePriceZincPowder));
            }
        }

        public double DealerPriceZincPowder
        {
            get => _dealerPriceZincPowder;
            set
            {
                _dealerPriceZincPowder = value;
                OnPropertyChanged(nameof(DealerPriceZincPowder));
            }
        }

        public double AreaWithColumns
        {
            get => _areaWithColumns;
            set
            {
                _areaWithColumns = value;
                OnPropertyChanged(nameof(AreaWithColumns));
            }
        }

        public double Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                OnPropertyChanged(nameof(Mass));
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        // Форматированный текст для копирования
        public string FormattedText =>
            $"{CalculationType}:\n" +
            $"Оптовая цена (цинк): {WholesalePriceZinc:N0} ₽\n" +
            $"Дилерская цена (цинк): {DealerPriceZinc:N0} ₽\n" +
            $"Стоимость завода (цинк): {FactoryCostZinc:N0} ₽\n" +
            $"Стоимость завода (цинк+порошок): {FactoryCostZincPowder:N0} ₽\n" +
            $"Оптовая цена (цинк+порошок): {WholesalePriceZincPowder:N0} ₽\n" +
            $"Дилерская цена (цинк+порошок): {DealerPriceZincPowder:N0} ₽\n" +
            (AreaWithColumns > 0 ? $"Площадь ограждения со столбом: {AreaWithColumns:F1} м²\n" : "") +
            $"Масса: {Mass:F0} кг, Объем: {Volume:F2} м³";

        // Свойство для проверки, нужно ли показывать площадь в UI
        public bool ShouldShowArea => AreaWithColumns > 0;


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName != nameof(FormattedText) && propertyName != nameof(ShouldShowArea))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShouldShowArea)));
            }
        }
    }

    // Новый класс для калькулятора УСП из круглой трубы
    public class USPRoundItem : INotifyPropertyChanged
    {
        private double _length;
        private double _width;
        private double _perimeter;
        private ObservableCollection<USPRoundCalculationResult> _calculations;

        public double Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged(nameof(Length));
                OnPropertyChanged(nameof(Perimeter));
                Calculate();
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Perimeter));
                Calculate();
            }
        }

        public double Perimeter
        {
            get => 2 * (Length + Width);
            private set
            {
                _perimeter = value;
                OnPropertyChanged(nameof(Perimeter));
            }
        }

        public ObservableCollection<USPRoundCalculationResult> Calculations
        {
            get => _calculations;
            private set
            {
                _calculations = value;
                OnPropertyChanged(nameof(Calculations));
            }
        }

        // Все типы расчетов УСП из круглой трубы
        private readonly string[] AllCalculationTypes = new[]
        {
            "УСП H=3м без встроенных ворот. Столбы Ф108",
            "УСП Н=3м (Н=4,1м в бросковой зоне) со встроенными воротами. Столбы Ф108",
            "УСП H=4,1м без встроенных ворот. Столбы Ф108",
            "УСП Н=4,1м со встроенными воротами. Столбы Ф108",
            "Дополнительная баскетбольная стойка",
            "Дополнительная калитка для высоты УСП 3м",
            "Дополнительная калитка для высоты УСП 4,1м"
        };

        public USPRoundItem()
        {
            Length = 0;
            Width = 0;

            _calculations = new ObservableCollection<USPRoundCalculationResult>();
            foreach (var type in AllCalculationTypes)
            {
                _calculations.Add(new USPRoundCalculationResult { CalculationType = type });
            }

            Calculate();
        }

        private void Calculate()
        {
            try
            {
                if (_calculations == null)
                    return;

                var coefficients = USPRoundPriceManager.GetCoefficients();
                var fixedValues = USPRoundPriceManager.GetFixedValues();

                foreach (var calculation in _calculations)
                {
                    switch (calculation.CalculationType)
                    {
                        case "УСП H=3м без встроенных ворот. Столбы Ф108":
                            Calculate3mWithoutGates(calculation, coefficients);
                            break;

                        case "УСП Н=3м (Н=4,1м в бросковой зоне) со встроенными воротами. Столбы Ф108":
                            Calculate3mWithGates(calculation, coefficients);
                            break;

                        case "УСП H=4,1м без встроенных ворот. Столбы Ф108":
                            Calculate41mWithoutGates(calculation, coefficients);
                            break;

                        case "УСП Н=4,1м со встроенными воротами. Столбы Ф108":
                            Calculate41mWithGates(calculation, coefficients);
                            break;

                        case "Дополнительная баскетбольная стойка":
                            CalculateBasketballStand(calculation, coefficients, fixedValues);
                            break;

                        case "Дополнительная калитка для высоты УСП 3м":
                            CalculateGate3m(calculation, coefficients, fixedValues);
                            break;

                        case "Дополнительная калитка для высоты УСП 4,1м":
                            CalculateGate41m(calculation, coefficients, fixedValues);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_calculations != null)
                {
                    foreach (var calculation in _calculations)
                    {
                        calculation.WholesalePriceZinc = 0;
                        calculation.DealerPriceZinc = 0;
                        calculation.FactoryCostZinc = 0;
                        calculation.FactoryCostZincPowder = 0;
                        calculation.WholesalePriceZincPowder = 0;
                        calculation.DealerPriceZincPowder = 0;
                        calculation.AreaWithColumns = 0;
                        calculation.Mass = 0;
                        calculation.Volume = 0;
                    }
                }
            }
        }

        private void Calculate3mWithoutGates(USPRoundCalculationResult result, USPRoundCoefficients coefficients)
        {
            var pricePerM2 = USPRoundPriceManager.GetPrice("УСП H=3м без встроенных ворот для мини футбола и баскетбольных щитов", "Столбы Ф108");
            var gatePrice = USPRoundPriceManager.GetPrice("Дополнительная калитка для высоты УСП 3м", "шт");

            // Формула: ((2*(Длина+Ширина)*3)*4868,8096)+33749,2400*2
            var baseCalculation = ((2 * (Length + Width) * 3) * pricePerM2) + (gatePrice * 2);
            result.FactoryCostZinc = baseCalculation;
            result.WholesalePriceZinc = baseCalculation * coefficients.WholesaleCoeff;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff;

            // Площадь: Периметр * 3
            result.AreaWithColumns = Perimeter * 3;

            // Расчет с порошковой покраской
            var powderCost = result.AreaWithColumns * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff;

            // Масса и объем
            result.Mass = result.AreaWithColumns * 15;
            result.Volume = result.AreaWithColumns * 0.06 * 1.2;
        }

        private void Calculate3mWithGates(USPRoundCalculationResult result, USPRoundCoefficients coefficients)
        {
            var pricePerM2 = USPRoundPriceManager.GetPrice("УСП H=3м без встроенных ворот для мини футбола и баскетбольных щитов", "Столбы Ф108");
            var gatePrice = USPRoundPriceManager.GetPrice("Комплект ворот 4,1 м с баскетболкой", "Столбы Ф108");

            // Формула: ((2*(Длина+Ширина-3)*3)*4868,8096+432427,4600)
            var baseCalculation = ((2 * (Length + Width - 3) * 3) * pricePerM2) + gatePrice;
            result.FactoryCostZinc = baseCalculation;
            result.WholesalePriceZinc = baseCalculation * coefficients.WholesaleCoeff;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff;

            // Площадь: такая же как без ворот
            result.AreaWithColumns = Perimeter * 3;

            // Расчет с порошковой покраской
            var powderCost = result.AreaWithColumns * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff;

            // Масса и объем с воротами
            result.Mass = (result.AreaWithColumns - 18) * 15 + 480 * 2;
            result.Volume = result.AreaWithColumns * 0.06 * 1.2 + 2;
        }

        private void Calculate41mWithoutGates(USPRoundCalculationResult result, USPRoundCoefficients coefficients)
        {
            var pricePerM2 = USPRoundPriceManager.GetPrice("УСП H=4,1м без встроенных ворот для мини футбола и баскетбольных щитов", "Столбы Ф108");
            var gatePrice = USPRoundPriceManager.GetPrice("Дополнительная калитка для высоты УСП 3м", "шт");

            // Формула: ((2*(Длина+Ширина)*4,1)*4880,1337)+33749,2400*2
            var baseCalculation = ((2 * (Length + Width) * 4.1) * pricePerM2) + (gatePrice * 2);
            result.FactoryCostZinc = baseCalculation;
            result.WholesalePriceZinc = baseCalculation * coefficients.WholesaleCoeff;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff;

            // Площадь: Периметр * 4,1
            result.AreaWithColumns = Perimeter * 4.1;

            // Расчет с порошковой покраской
            var powderCost = result.AreaWithColumns * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff;

            // Масса и объем
            result.Mass = result.AreaWithColumns * 16;
            result.Volume = result.AreaWithColumns * 0.06 * 1.2;
        }

        private void Calculate41mWithGates(USPRoundCalculationResult result, USPRoundCoefficients coefficients)
        {
            var pricePerM2 = USPRoundPriceManager.GetPrice("УСП H=4,1м без встроенных ворот для мини футбола и баскетбольных щитов", "Столбы Ф108");
            var gatePrice = USPRoundPriceManager.GetPrice("Комплект ворот 4,1 м с баскетболкой", "Столбы Ф108");

            // Формула: ((2*(Длина+Ширина-3)*4,1)*4880,1337)+432427,4600
            var baseCalculation = ((2 * (Length + Width - 3) * 4.1) * pricePerM2) + gatePrice;
            result.FactoryCostZinc = baseCalculation;
            result.WholesalePriceZinc = baseCalculation * coefficients.WholesaleCoeff;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff;

            // Площадь: такая же как без ворот
            result.AreaWithColumns = Perimeter * 4.1;

            // Расчет с порошковой покраской
            var powderCost = result.AreaWithColumns * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff;

            // Масса и объем с воротами
            result.Mass = (result.AreaWithColumns - 18) * 16 + 480 * 2;
            result.Volume = result.AreaWithColumns * 0.06 * 1.2 + 2;
        }

        private void CalculateBasketballStand(USPRoundCalculationResult result, USPRoundCoefficients coefficients, USPRoundFixedValues fixedValues)
        {
            var basePrice = USPRoundPriceManager.GetPrice("Дополнительная баскетбольная стойка", "шт");

            result.FactoryCostZinc = basePrice;
            result.WholesalePriceZinc = basePrice * coefficients.WholesaleCoeff * 1.51;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff / 1.13;

            // Площадь не рассчитывается
            result.AreaWithColumns = 0;

            // Расчет с порошковой покраской
            var powderCost = 5 * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder * 1.51;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff / 1.13;

            // Фиксированные масса и объем
            result.Mass = fixedValues.BasketballStand.Mass;
            result.Volume = fixedValues.BasketballStand.Volume;
        }

        private void CalculateGate3m(USPRoundCalculationResult result, USPRoundCoefficients coefficients, USPRoundFixedValues fixedValues)
        {
            var basePrice = USPRoundPriceManager.GetPrice("Дополнительная калитка для высоты УСП 3м", "шт");

            result.FactoryCostZinc = basePrice;
            result.WholesalePriceZinc = basePrice * coefficients.WholesaleCoeff;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff;

            // Площадь не рассчитывается
            result.AreaWithColumns = 0;

            // Расчет с порошковой покраской
            var powderCost = 6 * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff;

            // Фиксированные масса и объем
            result.Mass = fixedValues.Gate3m.Mass;
            result.Volume = fixedValues.Gate3m.Volume;
        }

        private void CalculateGate41m(USPRoundCalculationResult result, USPRoundCoefficients coefficients, USPRoundFixedValues fixedValues)
        {
            var basePrice = USPRoundPriceManager.GetPrice("Дополнительная калитка для высоты УСП 4,1м", "шт");

            result.FactoryCostZinc = basePrice;
            result.WholesalePriceZinc = basePrice * coefficients.WholesaleCoeff;
            result.DealerPriceZinc = result.WholesalePriceZinc / coefficients.DealerCoeff;

            // Площадь не рассчитывается
            result.AreaWithColumns = 0;

            // Расчет с порошковой покраской
            var powderCost = 6 * coefficients.PowderPricePerM2 * coefficients.PaintingSecondCoeff;
            result.FactoryCostZincPowder = (result.FactoryCostZinc + powderCost) * coefficients.PaintingCoeff;
            result.WholesalePriceZincPowder = coefficients.WholesaleCoeff * result.FactoryCostZincPowder;
            result.DealerPriceZincPowder = result.WholesalePriceZincPowder / coefficients.DealerCoeff;

            // Фиксированные масса и объем
            result.Mass = fixedValues.Gate41m.Mass;
            result.Volume = fixedValues.Gate41m.Volume;
        }

        public void RefreshPrices()
        {
            Calculate();
        }

        public void RecalculateAll()
        {
            Calculate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Новый класс для одного типа расчета УСП
    public class USPCalculationResult : INotifyPropertyChanged
    {
        private string _calculationType;
        private double _wholesalePrice;
        private double _dealerPrice;
        private double _areaWithColumns;
        private double _mass;
        private double _volume;

        public string CalculationType
        {
            get => _calculationType;
            set
            {
                _calculationType = value;
                OnPropertyChanged(nameof(CalculationType));
            }
        }

        public double WholesalePrice
        {
            get => _wholesalePrice;
            set
            {
                _wholesalePrice = value;
                OnPropertyChanged(nameof(WholesalePrice));
            }
        }

        public double DealerPrice
        {
            get => _dealerPrice;
            set
            {
                _dealerPrice = value;
                OnPropertyChanged(nameof(DealerPrice));
            }
        }

        public double AreaWithColumns
        {
            get => _areaWithColumns;
            set
            {
                _areaWithColumns = value;
                OnPropertyChanged(nameof(AreaWithColumns));
            }
        }

        public double Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                OnPropertyChanged(nameof(Mass));
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        // Форматированный текст для копирования
        public string FormattedText =>
            $"{CalculationType}:\n" +
            $"Оптовая цена: {WholesalePrice:N0} ₽\n" +
            $"Дилерская цена: {DealerPrice:N0} ₽\n" +
            $"Площадь ограждения со столбом: {AreaWithColumns:F1} м²\n" +
            $"Масса: {Mass:F0} кг, Объем: {Volume:F2} м³";

        // Свойство для проверки, нужно ли показывать площадь в UI
        public bool ShouldShowArea => AreaWithColumns > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Обновляем FormattedText только если это не он сам, чтобы избежать рекурсии
            if (propertyName != nameof(FormattedText))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedText)));
            }
        }
    }

    // Обновленный класс для калькулятора УСП
    public class USPItem : INotifyPropertyChanged
    {
        private double _length;
        private double _width;
        private double _perimeter;
        private string _height;
        private string _columnType;
        private bool _hasGates;
        private ObservableCollection<USPCalculationResult> _calculations;

        public double Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged(nameof(Length));
                OnPropertyChanged(nameof(Perimeter)); // Обновляем периметр при изменении длины
                Calculate();
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Perimeter)); // Обновляем периметр при изменении ширины
                Calculate();
            }
        }

        public double Perimeter
        {
            get => 2 * (Length + Width); // Автоматический расчет периметра
            private set
            {
                _perimeter = value;
                OnPropertyChanged(nameof(Perimeter));
            }
        }

        public string Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged(nameof(Height));
                Calculate();
            }
        }

        public string ColumnType
        {
            get => _columnType;
            set
            {
                _columnType = value;
                OnPropertyChanged(nameof(ColumnType));
                Calculate();
            }
        }

        public bool HasGates
        {
            get => _hasGates;
            set
            {
                _hasGates = value;
                OnPropertyChanged(nameof(HasGates));
                Calculate();
            }
        }

        public ObservableCollection<USPCalculationResult> Calculations
        {
            get => _calculations;
            private set
            {
                _calculations = value;
                OnPropertyChanged(nameof(Calculations));
            }
        }

        public string[] AvailableHeights => new[] { "3м", "4м" };
        public string[] AvailableColumnTypes => new[] { "80х80", "100х100" };

        public USPItem()
        {
            Length = 0;
            Width = 0;
            // Периметр теперь рассчитывается автоматически через свойство
            Height = "3м";
            ColumnType = "80х80";
            HasGates = false;

            // Инициализируем коллекцию расчетов ПЕРЕД вызовом Calculate()
            _calculations = new ObservableCollection<USPCalculationResult>();

            // Добавляем два типа расчетов
            _calculations.Add(new USPCalculationResult { CalculationType = "Без встроенных ворот" });
            _calculations.Add(new USPCalculationResult { CalculationType = "Со встроенными воротами для мини-футбола" });

            Calculate(); // Выполняем начальный расчет
        }

        private void Calculate()
        {
            try
            {
                if (_calculations == null)
                    return;

                var coefficients = USPPriceManager.GetCoefficients();

                // Рассчитываем оба типа одновременно
                foreach (var calculation in _calculations)
                {
                    switch (calculation.CalculationType)
                    {
                        case "Без встроенных ворот":
                            CalculateWithoutGates(calculation, coefficients);
                            break;

                        case "Со встроенными воротами для мини-футбола":
                            CalculateWithGates(calculation, coefficients);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки обнуляем результаты
                if (_calculations != null)
                {
                    foreach (var calculation in _calculations)
                    {
                        calculation.WholesalePrice = 0;
                        calculation.DealerPrice = 0;
                        calculation.AreaWithColumns = 0;
                        calculation.Mass = 0;
                        calculation.Volume = 0;
                    }
                }
            }
        }

        private void CalculateWithoutGates(USPCalculationResult result, USPCoefficients coefficients)
        {
            // Получение базовой цены за м²
            var pricePerM2 = USPPriceManager.GetUSPPrice(Height, ColumnType, false);

            // Высота для расчета (3м или 4м)
            var heightMultiplier = Height == "3м" ? 3 : 4;

            // Формула: ((2*(Длина+Ширина)*высота)*цена_за_м2)*коэф_оптовая
            var baseCalculation = ((2 * (Length + Width) * heightMultiplier) * pricePerM2);
            result.WholesalePrice = baseCalculation * coefficients.WholesaleCoeff;

            // Дилерская цена: Оптовая / коэф_дилера
            result.DealerPrice = result.WholesalePrice / coefficients.DealerCoeff;

            // Площадь ограждения со столбом: Периметр * высота
            result.AreaWithColumns = Perimeter * heightMultiplier;

            // Масса: Площадь * коэффициент_массы (зависит от высоты и типа столбов)
            var massMultiplier = GetMassMultiplier(Height, ColumnType);
            result.Mass = result.AreaWithColumns * massMultiplier;

            // Объем: Площадь * 0.06 * 1.2
            result.Volume = result.AreaWithColumns * 0.06 * 1.2;
        }

        private void CalculateWithGates(USPCalculationResult result, USPCoefficients coefficients)
        {
            // Получение базовой цены за м² и цены ворот
            var pricePerM2 = USPPriceManager.GetUSPPrice(Height, ColumnType, false);
            var gatePrice = USPPriceManager.GetUSPPrice(Height, ColumnType, true);

            // Высота для расчета (3м или 4м)
            var heightMultiplier = Height == "3м" ? 3 : 4;

            // Формула: ((2*(Длина+Ширина-3)*высота)*цена_за_м2 + цена_ворот)*коэф_оптовая
            var baseCalculation = ((2 * (Length + Width - 3) * heightMultiplier) * pricePerM2) + gatePrice;
            result.WholesalePrice = baseCalculation * coefficients.WholesaleCoeff;

            // Дилерская цена: Оптовая / коэф_дилера
            result.DealerPrice = result.WholesalePrice / coefficients.DealerCoeff;

            // Площадь ограждения со столбом: (Площадь без ворот соответствующего типа) + 14
            // Площадь без ворот = Периметр * высота
            var areaWithoutGatesForSameType = Perimeter * heightMultiplier;
            result.AreaWithColumns = areaWithoutGatesForSameType + 14;

            // Масса: Площадь * коэффициент_массы + 300 (масса ворот)
            var massMultiplier = GetMassMultiplier(Height, ColumnType);
            result.Mass = result.AreaWithColumns * massMultiplier + 300;

            // Объем: Площадь * 0.06 * 1.2 + 2 (объем ворот)
            result.Volume = result.AreaWithColumns * 0.06 * 1.2 + 2;
        }

        private double GetMassMultiplier(string height, string columnType)
        {
            // Коэффициенты массы согласно новым формулам:
            // УСП 3м, столбы 80х80: 15
            // УСП 3м, столбы 100х100: 16  
            // УСП 4м, столбы 80х80: 13
            // УСП 4м, столбы 100х100: 14.5

            if (height == "3м")
            {
                return columnType == "80х80" ? 15 : 16;
            }
            else // 4м
            {
                return columnType == "80х80" ? 13 : 14.5;
            }
        }

        public void RefreshPrices()
        {
            Calculate();
        }

        public void RecalculateAll()
        {
            Calculate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Новый класс для одного типа расчета хоккейной коробки
    public class HockeyCalculationResult : INotifyPropertyChanged
    {
        private string _calculationType;
        private double _purchaseCost;
        private int _sectionsCount;
        private int _netsCount;
        private double _mass;
        private double _volume;
        private double _dealerPrice;
        private double _wholesalePrice;
        private double _estimatePrice;

        public string CalculationType
        {
            get => _calculationType;
            set
            {
                _calculationType = value;
                OnPropertyChanged(nameof(CalculationType));
            }
        }

        public double PurchaseCost
        {
            get => _purchaseCost;
            set
            {
                _purchaseCost = value;
                OnPropertyChanged(nameof(PurchaseCost));
            }
        }

        public int SectionsCount
        {
            get => _sectionsCount;
            set
            {
                _sectionsCount = value;
                OnPropertyChanged(nameof(SectionsCount));
            }
        }

        public int NetsCount
        {
            get => _netsCount;
            set
            {
                _netsCount = value;
                OnPropertyChanged(nameof(NetsCount));
            }
        }

        public double Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                OnPropertyChanged(nameof(Mass));
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public double DealerPrice
        {
            get => _dealerPrice;
            set
            {
                _dealerPrice = value;
                OnPropertyChanged(nameof(DealerPrice));
            }
        }

        public double WholesalePrice
        {
            get => _wholesalePrice;
            set
            {
                _wholesalePrice = value;
                OnPropertyChanged(nameof(WholesalePrice));
            }
        }

        public double EstimatePrice
        {
            get => _estimatePrice;
            set
            {
                _estimatePrice = value;
                OnPropertyChanged(nameof(EstimatePrice));
            }
        }

        // Форматированный текст для копирования
        public string FormattedText =>
            $"{CalculationType}:\n" +
            $"Закупка: {PurchaseCost:N0} ₽\n" +
            $"Дилер: {DealerPrice:N0} ₽\n" +
            $"Оптовая: {WholesalePrice:N0} ₽\n" +
            $"Смета: {EstimatePrice:N0} ₽\n" +
            $"Секций: {SectionsCount} шт, Сеток: {NetsCount} шт\n" +
            $"Масса: {Mass:F1} кг, Объем: {Volume:F2} м³";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Обновляем FormattedText только если это не он сам, чтобы избежать рекурсии
            if (propertyName != nameof(FormattedText))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedText)));
            }
        }
    }

    // Обновленный класс для калькулятора хоккейных коробок
    public class HockeyRinkItem : INotifyPropertyChanged
    {
        private double _width;
        private double _length;
        private double _radius;
        private string _glassThickness;
        private string _netHeight;
        private ObservableCollection<HockeyCalculationResult> _calculations;

        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));
                Calculate();
            }
        }

        public double Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged(nameof(Length));
                Calculate();
            }
        }

        public double Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                OnPropertyChanged(nameof(Radius));
                Calculate();
            }
        }

        public string GlassThickness
        {
            get => _glassThickness;
            set
            {
                _glassThickness = value;
                OnPropertyChanged(nameof(GlassThickness));
                Calculate();
            }
        }

        public string NetHeight
        {
            get => _netHeight;
            set
            {
                _netHeight = value;
                OnPropertyChanged(nameof(NetHeight));
                Calculate();
            }
        }

        public ObservableCollection<HockeyCalculationResult> Calculations
        {
            get => _calculations;
            private set
            {
                _calculations = value;
                OnPropertyChanged(nameof(Calculations));
            }
        }

        public string[] AvailableRadiuses => new[] { "3,0", "4,0", "5,0", "7,5", "8,5" };


        public string[] AvailableGlassThicknesses => new[] { "5мм", "7мм" };
        public string[] AvailableNetHeights => new[] { "1,5м", "2м" };

        // Все доступные типы расчетов
        private readonly string[] AllCalculationTypes = new[]
        {
            "Без защитной сетки",
            "Сетка в бросковой зоне",
            "Сетка по периметру",
            "Защитная сетка в бросковой зоне (отдельно)",
            "Защитная сетка по периметру (отдельно)",
            "Дополнительная калитка",
            "Дополнительные тех. ворота"
        };

        public HockeyRinkItem()
        {
            Width = 0;
            Length = 0;
            Radius = 3.0;
            GlassThickness = "5мм";
            NetHeight = "1,5м";

            _calculations = new ObservableCollection<HockeyCalculationResult>();
            foreach (var type in AllCalculationTypes)
            {
                _calculations.Add(new HockeyCalculationResult { CalculationType = type });
            }

            Calculate();
        }

        private void Calculate()
        {
            try
            {
                // Проверяем, что коллекция инициализирована
                if (_calculations == null)
                    return;

                // Получение цен из менеджера
                var glassPrice = HockeyRinkPriceManager.GetGlassPrice(GlassThickness);
                var netPriceBroskovaya = HockeyRinkPriceManager.GetNetPrice("Защитная сетка в бросковой зоне", NetHeight);
                var netPricePerimeter = HockeyRinkPriceManager.GetNetPrice("Защитная сетка по периметру", NetHeight);
                var netPriceSeparate = HockeyRinkPriceManager.GetNetPrice("Защитная сетка при заказе отдельно", NetHeight);
                var gatePrice = HockeyRinkPriceManager.GetGatePrice();
                var techGatePrice = HockeyRinkPriceManager.GetTechGatePrice();

                var coefficients = HockeyRinkPriceManager.GetCoefficients();

                // Расчет количества секций по радиусу согласно формулам
                int radiusSections = GetRadiusSections(Radius);

                // Базовые расчеты согласно формулам Excel:
                // ОКРУГЛВВЕРХ(СУММ((Ширина-2*Радиус)/2);0)*2
                // ОКРУГЛВВЕРХ(СУММ((Длина-2*Радиус)/2);0)*2
                double widthAfterRadius = Math.Max(0, Width - 2 * Radius);
                double lengthAfterRadius = Math.Max(0, Length - 2 * Radius);

                int widthSections = widthAfterRadius > 0 ? (int)Math.Ceiling(widthAfterRadius / 2) * 2 : 0;
                int lengthSections = lengthAfterRadius > 0 ? (int)Math.Ceiling(lengthAfterRadius / 2) * 2 : 0;
                int totalBoardSections = widthSections + lengthSections + radiusSections;

                // Рассчитываем ВСЕ типы одновременно согласно точным формулам
                foreach (var calculation in _calculations)
                {
                    switch (calculation.CalculationType)
                    {
                        case "Без защитной сетки":
                            CalculateWithoutNets(calculation, totalBoardSections, glassPrice, coefficients);
                            break;

                        case "Сетка в бросковой зоне":
                            CalculateWithThrowingZoneNets(calculation, totalBoardSections, widthSections, radiusSections,
                                glassPrice, netPriceBroskovaya, coefficients);
                            break;

                        case "Сетка по периметру":
                            CalculateWithPerimeterNets(calculation, totalBoardSections, glassPrice, netPricePerimeter, coefficients);
                            break;

                        case "Защитная сетка в бросковой зоне (отдельно)":
                            CalculateSeparateThrowingZoneNets(calculation, widthSections, radiusSections,
                                netPriceSeparate, coefficients);
                            break;

                        case "Защитная сетка по периметру (отдельно)":
                            CalculateSeparatePerimeterNets(calculation, totalBoardSections, netPriceSeparate, coefficients);
                            break;

                        case "Дополнительная калитка":
                            CalculateAdditionalGate(calculation, gatePrice, coefficients);
                            break;

                        case "Дополнительные тех. ворота":
                            CalculateAdditionalTechGate(calculation, techGatePrice, coefficients);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки обнуляем результаты
                if (_calculations != null)
                {
                    foreach (var calculation in _calculations)
                    {
                        calculation.PurchaseCost = 0;
                        calculation.SectionsCount = 0;
                        calculation.NetsCount = 0;
                        calculation.Mass = 0;
                        calculation.Volume = 0;
                        calculation.DealerPrice = 0;
                        calculation.WholesalePrice = 0;
                        calculation.EstimatePrice = 0;
                    }
                }
            }
        }

        private int GetRadiusSections(double radius)
        {
            const double tolerance = 0.01;

            if (Math.Abs(radius - 3.0) < tolerance) return 12;
            if (Math.Abs(radius - 4.0) < tolerance) return 16;
            if (Math.Abs(radius - 5.0) < tolerance) return 16;
            if (Math.Abs(radius - 7.5) < tolerance) return 24;
            if (Math.Abs(radius - 8.5) < tolerance) return 28;

            return 12;
        }

        private void CalculateWithoutNets(HockeyCalculationResult result, int totalSections, double glassPrice,
            HockeyRinkCoefficients coefficients)
        {
            result.PurchaseCost = totalSections * glassPrice;
            result.SectionsCount = totalSections;
            result.NetsCount = 0;

            // Масса: стеклопластик по толщине (5мм=35кг, 7мм=36кг на секцию)
            double glassWeight = GlassThickness == "5мм" ? 35 : 36;
            result.Mass = glassWeight * result.SectionsCount;

            // Объем: 0.16 м³ на секцию
            result.Volume = result.SectionsCount * 0.16;

            // Цены (правильные формулы)
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        private void CalculateWithThrowingZoneNets(HockeyCalculationResult result, int totalSections, int widthSections, int radiusSections,
            double glassPrice, double netPrice, HockeyRinkCoefficients coefficients)
        {
            // Стоимость согласно формуле:
            // Стеклопластик для всех секций + сетки только в бросковой зоне (ширина + радиус)
            double glassCost = totalSections * glassPrice;
            int throwingZoneNets = widthSections + radiusSections;
            double netsCost = throwingZoneNets * netPrice;

            result.PurchaseCost = glassCost + netsCost;
            result.SectionsCount = totalSections;
            result.NetsCount = throwingZoneNets;

            // Масса: стеклопластик + сетки согласно формуле
            double glassWeight = GlassThickness == "5мм" ? 35 : 36;
            double netWeight = NetHeight == "1,5м" ? 22 : 30;
            result.Mass = (glassWeight * result.SectionsCount) + (netWeight * result.NetsCount);

            // Объем: стеклопластик + сетки согласно формуле
            double netVolumePerUnit = NetHeight == "1,5м" ? 0.2 : 0.25;
            result.Volume = (result.SectionsCount * 0.16) + (result.NetsCount * netVolumePerUnit);

            // Цены согласно формулам
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        private void CalculateWithPerimeterNets(HockeyCalculationResult result, int totalSections, double glassPrice, double netPrice,
            HockeyRinkCoefficients coefficients)
        {
            // Стоимость = (стеклопластик + сетка) за метр * количество секций
            double costPerMeter = glassPrice + netPrice;
            result.PurchaseCost = totalSections * costPerMeter;
            result.SectionsCount = totalSections;
            result.NetsCount = totalSections;

            // Масса
            double glassWeight = GlassThickness == "5мм" ? 35 : 36;
            double netWeight = NetHeight == "1,5м" ? 22 : 30;
            result.Mass = (glassWeight * result.SectionsCount) + (netWeight * result.NetsCount);

            // Объем
            double netVolumePerUnit = NetHeight == "1,5м" ? 0.2 : 0.25;
            result.Volume = (result.SectionsCount * 0.16) + (result.NetsCount * netVolumePerUnit);

            // Цены (правильные формулы)
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        private void CalculateSeparateThrowingZoneNets(HockeyCalculationResult result, int widthSections, int radiusSections,
            double netPrice, HockeyRinkCoefficients coefficients)
        {
            // Стоимость согласно формуле: только сетки в бросковой зоне (ширина + радиус)
            int throwingZoneNets = Math.Max(0, widthSections) + radiusSections;
            result.PurchaseCost = throwingZoneNets * netPrice;
            result.SectionsCount = 0; // НЕ РАСЧИТЫВАЕТСЯ согласно формулам
            result.NetsCount = throwingZoneNets;

            // Масса согласно формуле: только от сеток (стеклопластик = 0)
            double glassWeight = GlassThickness == "5мм" ? 35 : 36;
            double netWeight = NetHeight == "1,5м" ? 22 : 30;
            result.Mass = (glassWeight * 0) + (netWeight * result.NetsCount);

            // Объем согласно формуле: только от сеток (стеклопластик = 0)
            double netVolumePerUnit = NetHeight == "1,5м" ? 0.2 : 0.25;
            result.Volume = (0 * 0.16) + (result.NetsCount * netVolumePerUnit);

            // Цены согласно формулам
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        private void CalculateSeparatePerimeterNets(HockeyCalculationResult result, int totalSections, double netPrice,
            HockeyRinkCoefficients coefficients)
        {
            // Стоимость согласно формуле: все секции по периметру
            int safeTotalSections = Math.Max(0, totalSections);
            result.PurchaseCost = safeTotalSections * netPrice;
            result.SectionsCount = 0; // НЕ РАСЧИТЫВАЕТСЯ согласно формулам
            result.NetsCount = safeTotalSections;

            // Масса согласно формуле: только от сеток (стеклопластик = 0)
            double glassWeight = GlassThickness == "5мм" ? 35 : 36;
            double netWeight = NetHeight == "1,5м" ? 22 : 30;
            result.Mass = (glassWeight * 0) + (netWeight * result.NetsCount);

            // Объем согласно формуле: только от сеток (стеклопластик = 0)
            double netVolumePerUnit = NetHeight == "1,5м" ? 0.2 : 0.25;
            result.Volume = (0 * 0.16) + (result.NetsCount * netVolumePerUnit);

            // Цены согласно формулам
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        private void CalculateAdditionalGate(HockeyCalculationResult result, double gatePrice, HockeyRinkCoefficients coefficients)
        {
            // Стоимость согласно формуле: фиксированная цена 33000
            result.PurchaseCost = gatePrice;
            result.SectionsCount = 0; // НЕ РАСЧИТЫВАЕТСЯ согласно формулам
            result.NetsCount = 0; // НЕ РАСЧИТЫВАЕТСЯ согласно формулам

            // Формула массы: ((3,1415*2*Радиус)+2*(Ширина-2*Радиус))*1,5*9,5
            double widthAfterRadius = Math.Max(0, Width - 2 * Radius);
            double perimeter = (3.1415 * 2 * Radius) + 2 * widthAfterRadius;
            result.Mass = perimeter * 1.5 * 9.5;

            // Формула объема: (((3,1415*2*Радиус)+2*(Ширина-2*Радиус))*1,5*0,06)*1,2
            result.Volume = perimeter * 1.5 * 0.06 * 1.2;

            // Цены согласно формулам
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        private void CalculateAdditionalTechGate(HockeyCalculationResult result, double techGatePrice, HockeyRinkCoefficients coefficients)
        {
            // Стоимость согласно формуле: фиксированная цена 85000
            result.PurchaseCost = techGatePrice;
            result.SectionsCount = 0; // НЕ РАСЧИТЫВАЕТСЯ согласно формулам
            result.NetsCount = 0; // НЕ РАСЧИТЫВАЕТСЯ согласно формулам

            // Формула массы: ((3,1415*2*Радиус)+2*(Ширина-2*Радиус)+2*(Длина-2*Радиус))*1,5*9,5
            double widthAfterRadius = Math.Max(0, Width - 2 * Radius);
            double lengthAfterRadius = Math.Max(0, Length - 2 * Radius);
            double perimeter = (3.1415 * 2 * Radius) + 2 * widthAfterRadius + 2 * lengthAfterRadius;
            result.Mass = perimeter * 1.5 * 9.5;

            // Формула объема: ((3,1415*2*Радиус)+2*(Ширина-2*Радиус)+2*(Длина-2*Радиус))*1,5*0,06
            result.Volume = perimeter * 1.5 * 0.06;

            // Цены согласно формулам
            result.DealerPrice = result.PurchaseCost * coefficients.DealerCoeff;
            result.WholesalePrice = result.PurchaseCost * coefficients.WholesaleCoeff;
            result.EstimatePrice = result.WholesalePrice * coefficients.EstimateCoeff;
        }

        public void RefreshPrices()
        {
            Calculate();
        }

        // Публичный метод для пересчета
        public void RecalculateAll()
        {
            Calculate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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

    // Структура для коэффициентов хоккейных коробок
    public class HockeyRinkCoefficients
    {
        public double DealerCoeff { get; set; } = 1.25;
        public double WholesaleCoeff { get; set; } = 1.20;
        public double EstimateCoeff { get; set; } = 1.80;
    }

    // Структура для коэффициентов УСП
    public class USPCoefficients
    {
        public double WholesaleCoeff { get; set; } = 1.8;
        public double DealerCoeff { get; set; } = 1.3;
    }

    // Элемент цены для УСП
    public class USPPriceItem : INotifyPropertyChanged
    {
        private string _category;
        private string _subcategory;
        private string _name;
        private double _price;
        private string _unit;

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        public string Subcategory
        {
            get => _subcategory;
            set
            {
                _subcategory = value;
                OnPropertyChanged(nameof(Subcategory));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
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

        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                OnPropertyChanged(nameof(Unit));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Элемент цены для хоккейных коробок
    public class HockeyRinkPriceItem : INotifyPropertyChanged
    {
        private string _category;
        private string _subcategory;
        private string _name;
        private double _price;

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        public string Subcategory
        {
            get => _subcategory;
            set
            {
                _subcategory = value;
                OnPropertyChanged(nameof(Subcategory));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
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

    // Элемент цены для УСП из круглой трубы
    public class USPRoundPriceItem : INotifyPropertyChanged
    {
        private string _category;
        private string _subcategory;
        private string _name;
        private double _price;
        private string _unit;

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        public string Subcategory
        {
            get => _subcategory;
            set
            {
                _subcategory = value;
                OnPropertyChanged(nameof(Subcategory));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
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

        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                OnPropertyChanged(nameof(Unit));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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
                MessageBox.Show($"Ошибка инициализации цен УСП из круглой трубы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch
            {
                _isOnlineMode = false;
                Initialize();
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
                MessageBox.Show($"Ошибка инициализации цен УСП: {ex.Message}", "Ошибка",
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
                await SupabaseUSPPriceManager.InitializeAsync();
                _isOnlineMode = true;
            }
            catch
            {
                _isOnlineMode = false;
                Initialize(); // Fallback к локальному режиму
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

            if (_uspPrices != null && _uspPrices.ContainsKey(category) &&
                _uspPrices[category].ContainsKey(key))
            {
                return _uspPrices[category][key];
            }

            // Возвращаем значения по умолчанию
            if (withGates)
            {
                if (height == "3м")
                    return columnType == "80х80" ? 235699 : 248695;
                else
                    return columnType == "80х80" ? 245560 : 259498;
            }
            else
            {
                if (height == "3м")
                    return columnType == "80х80" ? 3349 : 3696;
                else
                    return columnType == "80х80" ? 3051 : 3341;
            }
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

    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d.ToString("F1", CultureInfo.CurrentCulture);
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                // Удаляем лишние пробелы
                s = s.Trim();

                // Пробуем разные варианты парсинга как в старом рабочем коде
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out var result1))
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно преобразовано '{s}' в {result1} (текущая культура)");
                    return result1;
                }

                if (double.TryParse(s.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно преобразовано '{s}' в {result2} (инвариантная культура)");
                    return result2;
                }

                if (double.TryParse(s.Replace(".", ","), NumberStyles.Float, CultureInfo.CurrentCulture, out var result3))
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно преобразовано '{s}' в {result3} (замена точки на запятую)");
                    return result3;
                }

                System.Diagnostics.Debug.WriteLine($"Не удалось преобразовать '{s}' в double");
            }
            return value;
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

            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
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
