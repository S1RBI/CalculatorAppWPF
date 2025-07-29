using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorApp
{
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
}
