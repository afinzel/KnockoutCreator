namespace Knockout
{
    using System;

    public interface IKoCreator
    {
        string PageName { get; set; }
        string GenerateJs(object callingObject);
        void AddBinding(string target);
        void AddSubscription(string targetPropertyName, string subFunctionName, string viewModel);        
        void AddJsSubscription(string targetPropertyName, string jsFunctionName);
        void AddViewModel(string vmName, Type vmType, string targetBinding = "", string jsFunctionName = "");
        void AddViewModel(string vmName, ViewModel viewModel, string targetBinding = "", string jsFunctionName = "");
    }
}
