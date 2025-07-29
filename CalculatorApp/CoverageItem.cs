using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorApp
{
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
}
