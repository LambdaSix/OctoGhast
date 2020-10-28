using System;

namespace OctoGhast.Framework {
    public class LoaderInfoAttribute : Attribute
    {
        public string FieldName { get; }
        public bool Required { get; }
        public object DefaultValue { get; }

        public int ExpectedCount { get; set; }
        public Type TypeLoader { get; set; }

        public LoaderInfoAttribute(string fieldName, bool required = false, object defaultValue = null)
        {
            FieldName = fieldName;
            Required = required;
            DefaultValue = defaultValue;
        }
    }
}