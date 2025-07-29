using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace CalculatorApp
{
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
