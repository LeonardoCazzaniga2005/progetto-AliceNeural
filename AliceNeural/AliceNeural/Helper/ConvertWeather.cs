using AliceNeural.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliceNeural.Helper
{
    internal class ConvertWeather
    {
        
        public static string ConvertWeatherCode(int? code)
        {
            string meteo = "";
            switch (code)
            {
                case int codice when (codice >= 50 && codice <= 67) || (codice >= 80 && codice <= 82) || (codice >= 95 && codice <= 99):
                    meteo = "pioggia";
                    break;
                case int codice when (codice == 0 || codice == 1):
                    meteo = "sole";
                    break;
                case int codice when (codice == 2 || codice == 3):
                    meteo = "nuvoloso";
                    break;
                case int codice when (codice >= 71 && codice <= 77) || (codice >= 85 && codice <= 86):
                    meteo = "neve";
                    break;
            }
            return meteo;
        }
    }
}
