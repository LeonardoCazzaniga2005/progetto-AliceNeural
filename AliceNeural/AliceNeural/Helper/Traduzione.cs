using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliceNeural.Helper
{
    public class Traduzione
    {
        public static string Traduci(string parola)
        {
            string? result = parola switch
            {
                "negozi" => "shop",
                "parcheggi" => "parking",
                "ristoranti" => "restaurants",
                "bar" => "bars",
                _ => "errore"
            };
            return result;
        }
    }
}
