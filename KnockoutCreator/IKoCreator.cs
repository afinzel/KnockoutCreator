namespace Knockout
{
    using System;

    public interface IKoCreator
    {
        string PageName { get; set; }
        string GenerateJs(object callingObject);
        void AddBinding(string target);
        void AddSubscription(string targetPropertyName, string subFunctionName);
        void AddViewModel(string vmName, Type vmType);
        void AddJsSubscription(string targetPropertyName, string jsFunctionName);
    }
}
