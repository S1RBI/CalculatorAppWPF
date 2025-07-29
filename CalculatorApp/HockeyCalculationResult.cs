using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorApp
{
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
}
