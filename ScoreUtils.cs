using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLoggerPlugin
{
    public static class ScoreUtils
    {
        public static double ScoreOfVital(int vital, int maxVital)
        {
            //四段折线
            if (vital <= 50)
                return 2.5 * vital;
            else if (vital <= 75)
                return 1.7 * (vital - 50) + ScoreOfVital(50, maxVital);
            else if (vital <= maxVital - 10)
                return 1.2 * (vital - 75) + ScoreOfVital(75, maxVital);
            else
                return 0.7 * (vital - (maxVital - 10)) + ScoreOfVital(maxVital - 10, maxVital);
        }
        public static int ReviseOver1200(int x)
        {
            return x > 1200 ? x * 2 - 1200 : x;
        }

    }
}
