using Mafrecal.WorkerService.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Helpers
{
    public class General
    {
        //public static (double NewValue, bool Adjusted) AdjustIfEndsWith5At3Decimals(double value)
        //{
        //    // Garante exatamente 3 casas decimais
        //    value = Math.Round(value, 3, MidpointRounding.AwayFromZero);

        //    // Escala para inteiro (3 casas)
        //    int scaled = (int)Math.Round(value * 1000, 0, MidpointRounding.AwayFromZero);

        //    // Verifica se a 3ª casa decimal é 5
        //    if (scaled % 10 == 5)
        //    {
        //        double newValue = value - 0.001;
        //        return (newValue, true);
        //    }

        //    return (value, false);
        //}

        public static (double NewValue, bool Adjusted) AdjustIfEndsWith5At3Decimals(double value)
        {
            // Arredonda a 3 casas (regra normal AwayFromZero)
            double rounded3 = Math.Round(value, 3, MidpointRounding.AwayFromZero);

            // Escala para inteiro para inspecionar dígitos
            int scaled3 = (int)Math.Round(rounded3 * 1000, 0, MidpointRounding.AwayFromZero);

            // Verifica se a 3ª casa é 5
            bool thirdDigitIs5 = scaled3 % 10 == 5;

            if (!thirdDigitIs5)
                return (value, false);

            /*
               Agora precisamos garantir que:
               - OU já era 5 nas 3 casas originais
               - OU veio de arredondamento (4ª casa >= 5)

               Para isso analisamos o valor com 4 casas
            */
            double rounded4 = Math.Round(value, 4, MidpointRounding.AwayFromZero);
            int scaled4 = (int)Math.Round(rounded4 * 10000, 0, MidpointRounding.AwayFromZero);

            int fourthDigit = scaled4 % 10;

            bool cameFromRoundingUp = fourthDigit >= 5;

            if (thirdDigitIs5)
            {
                double newValue = rounded3 - 0.005;
                return (newValue, true);
            }

            return (value, false);
        }



    }
}