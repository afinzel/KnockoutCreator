

namespace Knockout
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	
	public class KoCreator : IKoCreator
	{

		private string _bindingTarget;
		private IList<Subscription> _subscriptions = new List<Subscription>();
		private IList<JsSubscription> _jsSubscriptions = new List<JsSubscription>();        
		private IList<ViewModelToAdd> _viewModelsToAdd = new List<ViewModelToAdd>();

		public string PageName { get; set; }
	 

		public string GenerateJs(object callingObject)
		{
			return _viewModelsToAdd.Aggregate("", (current, curVM) => current + createJSForVM(curVM.ObjType, curVM.VmName, callingObject.GetType()));
		}

		private string createJSForVM(Type curType, string vmName, Type callingObject)
		{

			string returnJs = "";

			returnJs += "$(function() {";

			returnJs += CreateJSViewModel(curType);

			returnJs += "$(function() {var  " + vmName + " = ko.mapping.fromJS(baseModel);\n";
		  
			returnJs = GetMethodJavascript(returnJs, callingObject, vmName);

			returnJs += "window." + vmName + " = " + vmName + ";\n";

			returnJs += GetBindingJavascript(vmName);

			returnJs += GetDependabaleJavascript(vmName);

			returnJs += "});\n";
			returnJs += "});\n";

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
					var x = (KoMethodAttribute) methodInfo.GetCustomAttributes(typeof (KoMethodAttribute), false)[0];
					AddSubscription(x.ObservableField(), methodInfo.Name);
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
					returnJs += GetJSONCall(methodInfo.Name, vmName);
					returnJs += "\t}\n";
				}
			}
			return returnJs;
		}


		private string GetJSONCall(string methodName, string curType)
		{
			return "\tjQuery.ajax({url: '/" + PageName + "/" + methodName +
				   "',dataType:'json',contentType: 'application/json', type: \"post\", data: ko.toJSON(" + curType + "), success:function(" + methodName + "Result) {ko.mapping.updateFromJS(" + curType + ", " + methodName + "Result);\n}});";
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
								? fieldInfo.Name + ": ko.observable([])"
								: fieldInfo.Name + ": ko.observable()";
			}

			returnJs += "}\n";

			return returnJs;
		}

		public void AddBinding(string target)
		{
			_bindingTarget = target;

		}

		public void AddSubscription(string targetPropertyName, string subFunctionName)
		{
			_subscriptions.Add(new Subscription(targetPropertyName, subFunctionName));
		}

		private string GetBindingJavascript(string curType)
		{
			if (_bindingTarget == null)
			{
				return "ko.applyBindings(window." + curType + ");";
			}
			else
			{
				return "ko.applyBindings(window." + curType + ", jQuery(\"" + _bindingTarget + "\").get(0));";    
			}
			
		}

		private string GetDependabaleJavascript(string curType)
		{
			string result;
			result = "";
			
			if (_subscriptions != null)
			{
				result = _subscriptions.Aggregate("", (current, curSub) => current + (curType + "." + curSub.TargetProperty + ".subscribe(function(){window." + curType + "." + curSub.SubscriptionFunctionName + "()});\r\n"));
			}

			if (_jsSubscriptions != null)
			{
				result += _jsSubscriptions.Aggregate("", (current, curSub) => current + (curType + "." + curSub.TargetProperty + ".subscribe(function(){" + curSub.JsFunctionName + "()});\r\n"));
			}


			return result;
		}

		public void AddViewModel(string vmName, Type vmType)
		{
			_viewModelsToAdd.Add(new ViewModelToAdd(vmName, vmType));
		}

		public void AddJsSubscription(string targetPropertyName, string jsFunctionName)
		{          
			_jsSubscriptions.Add(new JsSubscription(targetPropertyName, jsFunctionName));
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
		private struct Subscription
		{
			public readonly string TargetProperty;
			public readonly string SubscriptionFunctionName;


			public Subscription(string targetProperty, string subscriptionFunctionName)
			{
				TargetProperty = targetProperty;
				SubscriptionFunctionName = subscriptionFunctionName;
			}
		}

		private struct ViewModelToAdd
		{
			public readonly string VmName;
			public readonly Type ObjType;

			public ViewModelToAdd(string vmName, Type obj)
			{
				VmName = vmName;
				ObjType = obj;
			}
		}


	}

	
}