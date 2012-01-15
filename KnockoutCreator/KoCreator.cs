

namespace Knockout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Web.Script.Serialization;

    public class KoCreator : IKoCreator
	{

		private string _bindingTarget;
		private IList<Subscription> _subscriptions = new List<Subscription>();
		private IList<JsSubscription> _jsSubscriptions = new List<JsSubscription>();        
		private IList<ViewModelToAdd> _viewModelsToAdd = new List<ViewModelToAdd>();
        private IList<JsFunction> _jsFunctions = new List<JsFunction>();
	    private ViewModel _initialValues;
        

		public string PageName { get; set; }
	 

		public string GenerateJs(object callingObject)
		{            
			return _viewModelsToAdd.Aggregate("", (current, curVM) => current + createJSForVM(curVM.ObjType, curVM.VmName, callingObject.GetType(), curVM.TargetBinding));
		}

        private string createJSForVM(Type curType, string vmName, Type callingObject, string targetBinding)
		{

			string returnJs = "";

			returnJs += "$(function() {";

			returnJs += CreateJSViewModel(curType);

            returnJs += GetJSFunctions();
            
            returnJs += "}\n";

			returnJs += "$(function() {var  " + vmName + " = ko.mapping.fromJS(baseModel);\n";

          
			returnJs = GetMethodJavascript(returnJs, callingObject, vmName);

			returnJs += "window." + vmName + " = " + vmName + ";\n";

			returnJs += GetBindingJavascript(vmName, targetBinding);

			returnJs += GetDependabaleJavascript(vmName);
            returnJs += GetPopulateInitialValuesJs(vmName);
           

			returnJs += "});\n";
			returnJs += "});\n";

			return returnJs;
		}

        private string GetPopulateInitialValuesJs(string curType)
	    {
	        var returnJs="";

            var serializer = new JavaScriptSerializer();
            
            returnJs += "$(document).ready(function() {\n";
            returnJs += "\n\tko.mapping.updateFromJS(" + curType + "," +  serializer.Serialize(_initialValues) + ")\n";
            returnJs += RunOnLoadJs();
            returnJs += "});";

	        return returnJs;
	    }

        private string RunOnLoadJs()
        {
            var returnJs = _viewModelsToAdd.Where(viewModel => viewModel.JsFunctionName.Length >= 1).Aggregate("", (current, viewModel) => current + (viewModel.JsFunctionName + "();\n"));            

            return returnJs ;
        }

	    private string GetJSFunctions()
        {
            string returnJs = "";

            foreach (var curJsFunction in _jsFunctions)
            {
                returnJs += ",";
                returnJs += curJsFunction.JsFunctionName + ":";
                returnJs += "\tfunction(){";                
                 returnJs += "\t\t" + curJsFunction.JsFunctionName + "();";                
                returnJs += "\t}";
            }

            return returnJs;

        }

		private string GetMethodJavascript(string returnJs, Type curType, string vmName)
		{
			var methodInfos =
				curType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);


			foreach (MethodInfo methodInfo in methodInfos)
			{
				if (methodInfo.GetCustomAttributes(typeof(KoMethodAttribute), false).Count() >= 1)
				{             
					var customAttribute = (KoMethodAttribute) methodInfo.GetCustomAttributes(typeof (KoMethodAttribute), false)[0];
                    if (customAttribute.ViewModel() == vmName)
                    {
                        if (customAttribute.ObservableField() != "")
                            AddSubscription(customAttribute.ObservableField(), methodInfo.Name, vmName);
                        returnJs += "\t" + vmName + "." + methodInfo.Name + " = function (";

					var firstRun = true;

					foreach (var parameterInfo in methodInfo.GetParameters())
					{

						if (firstRun)
						{
							firstRun = false;
						}
						else
						{
							returnJs += ",";
						}

						returnJs += parameterInfo.Name;
					}

                        returnJs += "){\n";
                        returnJs += GetJSONCall(methodInfo.Name, vmName, customAttribute.JavascriptFunctionToRunAfterMapping());
                        returnJs += "\t}\n";
                    }
				}
			}
			return returnJs;
		}


		private string GetJSONCall(string methodName, string curType, string javascriptToRunAfterBinding)
		{

		    var returnJS = "\n\t\tjQuery.post('/" + PageName + "/" + methodName +
		                   ".Rails','incomObject='+ ko.toJSON(" + curType + ")  ,function(" + methodName + "Result) {\n " +
		                   " \t\t ko.mapping.updateFromJS(" + curType + ", " + methodName +
		                   "Result);\n";


            if (javascriptToRunAfterBinding.Length >= 1)
            {
                returnJS += "\t\t" + javascriptToRunAfterBinding + "();\n";
            }
		    returnJS += "\t}\n,\"json\");"; 
		    return returnJS;
		}

		private string CreateJSViewModel(Type curType)
		{
			string returnJs = "var baseModel = {";

			var firstRun = true;

			foreach (var fieldInfo in curType.GetProperties())
			{
				if (firstRun)
				{
					firstRun = false;
				}
				else
				{
					returnJs += ",\n";
				}

				returnJs += typeof(Array).IsAssignableFrom(fieldInfo.PropertyType)
                                ? fieldInfo.Name + ": ko.observableArray([])"
								: fieldInfo.Name + ": ko.observable()";
			}
			

			return returnJs;
		}

		public void AddBinding(string target)
		{
			_bindingTarget = target;

		}

	    public void AddSubscription(string targetPropertyName, string subFunctionName, string viewModel)
		{
            _subscriptions.Add(new Subscription(targetPropertyName, subFunctionName, viewModel));
		}

        

        private string GetBindingJavascript(string curType, string targetBinding)
		{
            if (targetBinding == "")
			{
				return "ko.applyBindings(window." + curType + ");";
			}
			else
			{
                return "ko.applyBindings(window." + curType + ", $(\"" + targetBinding + "\").get(0));";    
			}
			
		}

		private string GetDependabaleJavascript(string viewModelName)
		{
			string result;
			result = "";
			
			if (_subscriptions != null)
			{
                result = _subscriptions.Where(func => func.ViewModel == viewModelName).Aggregate("", (current, subscription) => current + (viewModelName + "." + subscription.TargetProperty + ".subscribe(function(newValue){window." + viewModelName + "." + subscription.SubscriptionFunctionName + "()});\r\n"));
			}

			if (_jsSubscriptions != null)
			{
				result += _jsSubscriptions.Aggregate("", (current, curSub) => current + (viewModelName + "." + curSub.TargetProperty + ".subscribe(function(newValue){" + curSub.JsFunctionName + "()});\r\n"));
			}


			return result;
		}

        public void AddViewModel(string vmName, Type vmType, string targetBinding = "", string jsFunctionName = "")
		{
            _viewModelsToAdd.Add(new ViewModelToAdd(vmName, vmType, targetBinding, jsFunctionName));
		}


        public void AddViewModel(string vmName, ViewModel viewModel, string targetBinding = "", string jsFunctionName = "")
        {
            _viewModelsToAdd.Add(new ViewModelToAdd(vmName, viewModel.GetType(), targetBinding, jsFunctionName));
            _initialValues = viewModel;
        }

		public void AddJsSubscription(string targetPropertyName, string jsFunctionName)
		{          
			_jsSubscriptions.Add(new JsSubscription(targetPropertyName, jsFunctionName));
		}

        public void AddJsFunction(string propertyName, string jsFunctionName)
        {
            _jsFunctions.Add(new JsFunction(propertyName, jsFunctionName));
        }


		private struct JsSubscription
		{
			public readonly string TargetProperty;
			public readonly string JsFunctionName;


			public JsSubscription(string targetProperty, string jsFunctionName)
			{
				TargetProperty = targetProperty;
				JsFunctionName = jsFunctionName;
			}
		}

        private struct JsFunction
        {
            public readonly string PropertyName;
            public readonly string JsFunctionName;


            public JsFunction(string propertyName, string jsFunctionName)
            {
                PropertyName = propertyName;
                JsFunctionName = jsFunctionName;
            }
        }

		private struct Subscription
		{
			public readonly string TargetProperty;
			public readonly string SubscriptionFunctionName;
            public readonly string ViewModel;

            public Subscription(string targetProperty, string subscriptionFunctionName, string viewModel)
			{
				TargetProperty = targetProperty;
				SubscriptionFunctionName = subscriptionFunctionName;
			    ViewModel = viewModel;
			}
		}

		private struct ViewModelToAdd
		{
			public readonly string VmName;
			public readonly Type ObjType;
		    public string TargetBinding;
            public readonly string JsFunctionName;

		    public ViewModelToAdd(string vmName, Type obj, string targetBinding = "", string jsFunctionName = "")
			{
				VmName = vmName;
				ObjType = obj;
			    TargetBinding = targetBinding;
			    JsFunctionName = jsFunctionName;
			}


		}


	}

	
}