using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorApp
{
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
}
