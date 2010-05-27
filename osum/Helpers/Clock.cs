﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace osum.Helpers
{
    public static class Clock
    {
        // measured in seconds
        private static double time = 0;

        /// <summary>
        /// Get the current game time in milliseconds.
        /// </summary>
        public static int Time
        {
            get { return (int)(time * 1000); }
        }

        public static void Update(double elapsed)
        {
            time += elapsed;
        }
    }
}
