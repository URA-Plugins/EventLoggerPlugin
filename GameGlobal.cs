﻿using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLoggerPlugin
{
    public static class GameGlobal
    {
        public static readonly int[] TrainIds = [101, 105, 102, 103, 106];
        public static readonly Dictionary<int, int> ToTrainIndex = new Dictionary<int, int>
        {
            { 1101, 0 },
            { 1102, 1 },
            { 1103, 2 },
            { 1104, 3 },
            { 1105, 4 },
            { 601, 0 },
            { 602, 1 },
            { 603, 2 },
            { 604, 3 },
            { 605, 4 },
            { 101, 0 },
            { 105, 1 },
            { 102, 2 },
            { 103, 3 },
            { 106, 4 },
            { 2101, 0 },
            { 2201, 0 },
            { 2301, 0 },
            { 2102, 1 },
            { 2202, 1 },
            { 2302, 1 },
            { 2103, 2 },
            { 2203, 2 },
            { 2303, 2 },
            { 2104, 3 },
            { 2204, 3 },
            { 2304, 3 },
            { 2105, 4 },
            { 2205, 4 },
            { 2305, 4 },
            { 901, 0 },
            { 902, 2 },
            { 906, 4 }
        };
        public static readonly Dictionary<int, int> ToTrainId = new Dictionary<int, int>
        {
            [1101] = 101,
            [1102] = 105,
            [1103] = 102,
            [1104] = 103,
            [1105] = 106,
            [601] = 101,
            [602] = 105,
            [603] = 102,
            [604] = 103,
            [605] = 106,
            [101] = 101,
            [105] = 105,
            [102] = 102,
            [103] = 103,
            [106] = 106,
            [2101] = 101,
            [2201] = 101,
            [2301] = 101,
            [2102] = 105,
            [2202] = 105,
            [2302] = 105,
            [2103] = 102,
            [2203] = 102,
            [2303] = 102,
            [2104] = 103,
            [2204] = 103,
            [2304] = 103,
            [2105] = 106,
            [2205] = 106,
            [2305] = 106,
            [901] = 101,
            [902] = 102,
            [906] = 106
        };
    }
}
