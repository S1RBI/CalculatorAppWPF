// Вставьте сюда код MainWindow.xaml.cs
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
        private Storyboard _loadingStoryboard;

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

            // Запускаем анимацию загрузки
            StartLoadingAnimation();
            _ = InitializeWithLoadingAsync();
        }

        private async Task InitializeWithLoadingAsync()
        {
            try
            {
                // Параллельная загрузка всех данных для ускорения
                UpdateLoadingStatus("Подключение к облачным сервисам...");
                UpdateLoadingProgress(10);

                var loadingTasks = new[]
                {
                    Task.Run(async () => {
                        try
                        {
                            // Пытаемся загрузить из облака
                            await PriceManager.InitializeWithCloudAsync();
                            Dispatcher.Invoke(() => CompleteLoadingTask("✓ Цены на покрытия загружены"));
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Переход в локальный режим для покрытий: {ex.Message}");
                            // Переходим в локальный режим без предупреждений
                            Dispatcher.Invoke(() => {
                                PriceManager.LoadPrices(); // Загружаем локальные данные
                                CompleteLoadingTask("✓ Покрытия: локальный режим");
                            });
                            return false;
                        }
                    }),
                    Task.Run(async () => {
                        try
                        {
                            await HockeyRinkPriceManager.InitializeWithCloudAsync();
                            Dispatcher.Invoke(() => CompleteLoadingTask("✓ Цены хоккейных коробок загружены"));
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Переход в локальный режим для хоккея: {ex.Message}");
                            Dispatcher.Invoke(() => {
                                HockeyRinkPriceManager.Initialize(); // Загружаем локальные данные
                                CompleteLoadingTask("✓ Хоккей: локальный режим");
                            });
                            return false;
                        }
                    }),
                    Task.Run(async () => {
                        try
                        {
                            await USPPriceManager.InitializeWithCloudAsync();
                            Dispatcher.Invoke(() => CompleteLoadingTask("✓ Цены УСП загружены"));
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Переход в локальный режим для УСП: {ex.Message}");
                            Dispatcher.Invoke(() => {
                                USPPriceManager.Initialize(); // Загружаем локальные данные
                                CompleteLoadingTask("✓ УСП: локальный режим");
                            });
                            return false;
                        }
                    }),
                    Task.Run(async () => {
                        try
                        {
                            await USPRoundPriceManager.InitializeWithCloudAsync();
                            Dispatcher.Invoke(() => CompleteLoadingTask("✓ Цены УСП из круглой трубы загружены"));
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Переход в локальный режим для УСП круглая труба: {ex.Message}");
                            Dispatcher.Invoke(() => {
                                USPRoundPriceManager.Initialize(); // Загружаем локальные данные
                                CompleteLoadingTask("✓ УСП круглая труба: локальный режим");
                            });
                            return false;
                        }
                    })
                };

                // Отслеживаем прогресс и результаты
                var completedTasks = 0;
                var cloudResults = new bool[4]; // Результаты для каждого менеджера
                for (int i = 0; i < loadingTasks.Length; i++)
                {
                    cloudResults[i] = await loadingTasks[i];
                    completedTasks++;
                    UpdateLoadingProgress(20 + (completedTasks * 15)); // 20, 35, 50, 65
                }

                // Обновление заголовка окна
                UpdateLoadingStatus("Настройка интерфейса...");
                UpdateLoadingProgress(90);

                // Определяем режимы работы на основе фактических результатов
                var priceMode = cloudResults[0] ? "Облачный режим" : "Локальный режим";
                var hockeyMode = cloudResults[1] ? "Облачный режим" : "Локальный режим";
                var uspMode = cloudResults[2] ? "Облачный режим" : "Локальный режим";
                var uspRoundMode = cloudResults[3] ? "Облачный режим" : "Локальный режим";

                // Проверяем, все ли в одном режиме
                var allInSameMode = cloudResults.All(r => r) || cloudResults.All(r => !r);
                if (allInSameMode)
                {
                    var modeText = cloudResults[0] ? "Облачный режим" : "Локальный режим";
                    this.Title = $"Калькулятор покрытий - {modeText}";
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
                System.Diagnostics.Debug.WriteLine($"Критическая ошибка инициализации: {ex.Message}");
                UpdateLoadingStatus("Инициализация в локальном режиме");
                CompleteLoadingTask("✓ Локальный режим активирован");

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
                }
            };

            tasksList.Children.Add(taskItem);

            // Быстрая анимация без задержек
            taskItem.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        }

        private void CompleteLoading()
        {
            Dispatcher.Invoke(() =>
            {
                // Останавливаем анимацию загрузки
                StopLoadingAnimation();

                var loadingOverlay = FindName("LoadingOverlay") as FrameworkElement;
                if (loadingOverlay != null)
                {
                    // Быстрое скрытие экрана загрузки
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
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

        // Метод для запуска анимации загрузки
        private void StartLoadingAnimation()
        {
            try
            {
                // Создаем storyboard для анимации
                _loadingStoryboard = new Storyboard();
                _loadingStoryboard.RepeatBehavior = RepeatBehavior.Forever;

                // Анимация для первого кольца (самое большое, медленное)
                var rotation1 = new DoubleAnimation()
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(2),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTargetName(rotation1, "LoadingRotation1");
                Storyboard.SetTargetProperty(rotation1, new PropertyPath(RotateTransform.AngleProperty));

                // Анимация для второго кольца (среднее, быстрее, против часовой стрелки)
                var rotation2 = new DoubleAnimation()
                {
                    From = 360,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(1.5),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTargetName(rotation2, "LoadingRotation2");
                Storyboard.SetTargetProperty(rotation2, new PropertyPath(RotateTransform.AngleProperty));

                // Анимация для третьего кольца (самое маленькое, самое быстрое)
                var rotation3 = new DoubleAnimation()
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(1),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTargetName(rotation3, "LoadingRotation3");
                Storyboard.SetTargetProperty(rotation3, new PropertyPath(RotateTransform.AngleProperty));

                // Добавляем анимации в storyboard
                _loadingStoryboard.Children.Add(rotation1);
                _loadingStoryboard.Children.Add(rotation2);
                _loadingStoryboard.Children.Add(rotation3);

                // Запускаем анимацию
                _loadingStoryboard.Begin(this);
            }
            catch (Exception ex)
            {
                // В случае ошибки анимации просто игнорируем - приложение должно работать
                System.Diagnostics.Debug.WriteLine($"Ошибка запуска анимации загрузки: {ex.Message}");
            }
        }

        // Метод для остановки анимации загрузки
        private void StopLoadingAnimation()
        {
            try
            {
                _loadingStoryboard?.Stop(this);
            }
            catch (Exception ex)
            {
                // В случае ошибки анимации просто игнорируем
                System.Diagnostics.Debug.WriteLine($"Ошибка остановки анимации загрузки: {ex.Message}");
            }
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
                    _currentUser.Id = "supabaseuser"; // Можно получить реальный ID из Supabase если нужно
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
            try
            {
                System.Diagnostics.Debug.WriteLine("Начинаем загрузку УСП данных для админ-панели");
                var uspPrices = USPPriceManager.GetAllPrices();
                _uspPriceItems = new ObservableCollection<USPPriceItem>(uspPrices);

                var uspPriceItemsControl = FindName("USPPriceItemsControl") as ItemsControl;
                if (uspPriceItemsControl != null)
                    uspPriceItemsControl.ItemsSource = _uspPriceItems;

                // Загружаем коэффициенты
                var coefficients = USPPriceManager.GetCoefficients();
                var uspWholesaleCoeffTextBox = FindName("USPWholesaleCoeffTextBox") as TextBox;
                var uspDealerCoeffTextBox = FindName("USPDealerCoeffTextBox") as TextBox;

                if (uspWholesaleCoeffTextBox != null)
                    uspWholesaleCoeffTextBox.Text = coefficients.WholesaleCoeff.ToString();
                if (uspDealerCoeffTextBox != null)
                    uspDealerCoeffTextBox.Text = coefficients.DealerCoeff.ToString();

                System.Diagnostics.Debug.WriteLine($"УСП админ-панель: загружено {uspPrices.Count} позиций");
                System.Diagnostics.Debug.WriteLine($"УСП админ-панель: режим работы = {USPPriceManager.GetModeString()}");
                System.Diagnostics.Debug.WriteLine($"УСП админ-панель: коэффициенты = оптовый:{coefficients.WholesaleCoeff}, дилерский:{coefficients.DealerCoeff}");

                // Выводим первые несколько позиций для проверки
                foreach (var price in uspPrices.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"УСП позиция: {price.Category} - {price.Subcategory} = {price.Price}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка LoadUSPPricesForAdmin: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки УСП данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                if (_uspPriceItems == null || !_uspPriceItems.Any())
                {
                    MessageBox.Show("Нет данных для сохранения. Перезагрузите админ-панель.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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
                    var uspWholesaleCoeff = FindName("USPWholesaleCoeffTextBox") as TextBox;
                    var uspDealerCoeff = FindName("USPDealerCoeffTextBox") as TextBox;

                    var coefficients = new USPCoefficients
                    {
                        WholesaleCoeff = double.TryParse(uspWholesaleCoeff?.Text, out var wholesale) ? wholesale : 1.8,
                        DealerCoeff = double.TryParse(uspDealerCoeff?.Text, out var dealer) ? dealer : 1.3
                    };

                    System.Diagnostics.Debug.WriteLine($"Сохраняем УСП: {_uspPriceItems.Count} позиций, коэффициенты: {coefficients.WholesaleCoeff}/{coefficients.DealerCoeff}");

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
            try
            {
                var priceMode = PriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
                var hockeyMode = HockeyRinkPriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
                var uspMode = USPPriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";
                var uspRoundMode = USPRoundPriceManager.IsOnlineMode() ? "Облачный режим" : "Локальный режим";

                // Добавляем информацию о количестве загруженных данных
                var uspPricesCount = USPPriceManager.GetAllPrices().Count;
                var uspCoefficientsInfo = USPPriceManager.GetCoefficients();

                var info = $"РЕЖИМЫ РАБОТЫ:\n" +
                    $"Покрытия: {priceMode}\n" +
                    $"Хоккейные коробки: {hockeyMode}\n" +
                    $"УСП: {uspMode} (позиций: {uspPricesCount})\n" +
                    $"УСП из круглой трубы: {uspRoundMode}\n\n" +
                    $"КОЭФФИЦИЕНТЫ УСП:\n" +
                    $"Оптовый: {uspCoefficientsInfo.WholesaleCoeff}\n" +
                    $"Дилерский: {uspCoefficientsInfo.DealerCoeff}\n\n" +
                    $"ВЕРСИИ ДАННЫХ:\n" +
                    $"Покрытия: {SupabasePriceManager.GetCurrentVersion()}\n" +
                    $"Хоккейные цены: {SupabaseHockeyPriceManager.GetCurrentHockeyVersion()}\n" +
                    $"Коэфф. хоккея: {SupabaseHockeyPriceManager.GetCurrentCoefficientsVersion()}\n" +
                    $"УСП данные: {SupabaseUSPPriceManager.GetCurrentUSPVersion()}\n" +
                    $"УСП коэфф.: {SupabaseUSPPriceManager.GetCurrentUSPCoefficientsVersion()}\n" +
                    $"УСП круглая данные: {SupabaseUSPRoundPriceManager.GetCurrentUSPRoundVersion()}\n" +
                    $"УСП круглая коэфф.: {SupabaseUSPRoundPriceManager.GetCurrentUSPRoundCoefficientsVersion()}\n" +
                    $"УСП фикс. значения: {SupabaseUSPRoundPriceManager.GetCurrentUSPRoundFixedValuesVersion()}";

                MessageBox.Show(info, "Информация о системе", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения системной информации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

    #endregion
}
