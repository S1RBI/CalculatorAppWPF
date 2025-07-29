using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorApp
{
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
}
