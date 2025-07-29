using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorApp
{
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
}
