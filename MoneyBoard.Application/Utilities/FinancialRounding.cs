using System;

namespace MoneyBoard.Application.Utilities
{
    public static class FinancialRounding
    {
        /// <summary>
        /// Rounds a decimal value to 2 decimal places using Banker's Rounding (Round Half to Even).
        /// This is the standard for financial calculations to avoid cumulative bias.
        /// </summary>
        /// <param name="value">The decimal value to round</param>
        /// <returns>Rounded decimal value to 2 decimal places</returns>
        public static decimal RoundToCurrency(this decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Rounds a double value to 2 decimal places using Banker's Rounding.
        /// Converts to decimal first for precision.
        /// </summary>
        /// <param name="value">The double value to round</param>
        /// <returns>Rounded decimal value to 2 decimal places</returns>
        public static decimal RoundToCurrency(this double value)
        {
            return Math.Round((decimal)value, 2, MidpointRounding.ToEven);
        }
    }
}
