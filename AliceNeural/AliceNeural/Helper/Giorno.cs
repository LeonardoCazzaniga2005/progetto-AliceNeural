using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliceNeural.Helper
{
    internal class Giorno
    {
        public static int CalcolaGiorno(string giorno)
        {
            int g = 0;
            switch (giorno)
            {
                case "oggi":
                    g = 0;
                    break;
                case "domani":
                    g = 1;
                    break;
                case "dopodomani":
                    g = 2;
                    break;
            }
            return g;
        }
    }
}
