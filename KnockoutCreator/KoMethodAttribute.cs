using System;

namespace Knockout
{
    [AttributeUsage(AttributeTargets.Method)]
    public class KoMethodAttribute : System.Attribute
    {
        public KoMethodAttribute(string viewModel, string observableField, string javascriptFunctionToRunAfterMapping)
        {
            _observableField = observableField;
            _viewModel = viewModel;
            _javascriptFunctionToRunAfterMapping = javascriptFunctionToRunAfterMapping;
        }

        public string ObservableField()
        {
            return _observableField;
        }

        public string ViewModel()
        {
            return _viewModel;
        }

        public string JavascriptFunctionToRunAfterMapping()
        {
            return _javascriptFunctionToRunAfterMapping;
        }


        private readonly string  _observableField ;
        private readonly string _viewModel;
        private readonly string _javascriptFunctionToRunAfterMapping;
    }
}
