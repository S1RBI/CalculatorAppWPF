using System.ComponentModel;


namespace CalculatorApp
{
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
}
