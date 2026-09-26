namespace Game.Seasons
{
    public static class SeasonTemperatureUtility
    {
        public static float FahrenheitToCelsius(float fahrenheit) => (fahrenheit - 32f) * (5f / 9f);

        public static float CelsiusToFahrenheit(float celsius) => celsius * (9f / 5f) + 32f;

        public static float Convert(float value, TemperatureUnit from, TemperatureUnit to)
        {
            if (from == to)
                return value;

            return from == TemperatureUnit.Fahrenheit
                ? FahrenheitToCelsius(value)
                : CelsiusToFahrenheit(value);
        }

        public static string Format(float fahrenheit, TemperatureUnit displayUnit, string format = "0")
        {
            float value = displayUnit == TemperatureUnit.Fahrenheit
                ? fahrenheit
                : FahrenheitToCelsius(fahrenheit);
            string suffix = displayUnit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
            return value.ToString(format) + suffix;
        }
    }
}
