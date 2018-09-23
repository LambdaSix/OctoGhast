using System;
using System.Globalization;
using NGettext;

namespace OctoGhast.Translation {
    public static class Translation {
        private static ICatalog _catalog;

        /// <summary>
        /// Initialize the catalog with a given culture for the core strings library.
        /// </summary>
        /// <param name="culture"></param>
        public static void LoadTranslations(CultureInfo culture) {
            // This particular Translation instance is designed for the Core engine.
            // Modules should provide their own version of Translation using the modules translation files.
            _catalog = new Catalog("Core", "./locale", culture);
        }

        /// <summary>
        /// `using static Translation;` allows _($"String to translate, with {interpolation}")
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _(FormattableString str) {
            return String.Format(GetTranslation(str.Format), str.GetArguments());
        }

        public static string _(FormattableString singular, FormattableString plural, int amount) {
            return String.Format(GetTranslationPlural(singular.Format, plural.Format, amount, singular.GetArguments()));
        }

        private static string GetTranslation(string str) {
            // TODO: Hook up Gettext here
            return _catalog != null ? _catalog.GetString(str) : str;
        }

        private static string GetTranslationPlural(string singular, string plural, int amount, params object[] args) {
            if (_catalog != null) {
                return _catalog.GetPluralString(singular, plural, amount, args);
            }

            if (amount <= 1) {
                return String.Format(singular, args);
            }
            else {
                return String.Format(plural, args);
            }
        }
    }
}