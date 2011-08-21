using System;

namespace Knockout
{
    [AttributeUsage(AttributeTargets.Method)]
    public class KoMethodAttribute : System.Attribute
    {
        public KoMethodAttribute(string observableField)
        {
            _observableField = observableField;
        }

        public string ObservableField()
        {
            return _observableField;
        }


        private readonly string  _observableField ;
    }
}
