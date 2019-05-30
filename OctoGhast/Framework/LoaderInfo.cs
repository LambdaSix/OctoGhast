using System;

namespace OctoGhast.Framework {
    public class LoaderInfoAttribute : Attribute
    {
        public string FieldName { get; }
        public bool Required { get; }
        public object DefaultValue { get; }

        public int ExpectedCount { get; }
        public Type TypeLoader { get; set; }

        public LoaderInfoAttribute(string fieldName, bool required = false, object defaultValue = null, int expectedCount = -1)
        {
            FieldName = fieldName;
            Required = required;
            DefaultValue = defaultValue;
            ExpectedCount = expectedCount;
        }
    }
}