Knockout Creator
================

Knockout.js (http://knockoutjs.com/) is an opensource javascript library that simplifies UI and javascript.  If you are using c#, you end up duplicating the view model, both in Javascript and in c#.  This library will convert your c# viewmodel, straight into knockout viewmodel.

Creating your viewmodel
-----------------------
namespace knockoutExample
{
	public class Restaurant : Knockout.ViewModel
	{
	        public int RestaurantId { get; set; }
		public string Name { get; set; }

	}
}

Setting up your controller
--------------------------

public string KnockOutJs() {
	restViewModel = new Restaurant();
	koCreator = new Knockout.KoCreator()

	//Set your controller name
	koCreator.PageName = "Home";

	//Add your viewmodel Restaurant is the name of the model we want in javascript
	koCreator.AddViewModel("Restaurant", restViewModel.GetType());

	//Add a javascript function subscription.  This will call the javascript function test() everytime the name is changed
	koCreator.AddJsSubscription("Name","test");

	//This will return the javascript, we pass in the controller so that Knockout Creator can bind subscriptions
	return koCreator.GenerateJs(this);
}

Creating a method to be called using AJAX
-----------------------------------------

//Add a custom attribute specifying the variable that when changed will trigger this via AJAX
[KoMethod("RestaurantId")]

//The method accepts one attribute, our viewmodel.  This will be passed back using AJAX
public JsonResult GetRestaurantById(knockoutExample.Restaurant viewModel) {

	//get the restaurantId that has been posted
	int restaurantId = viewModel.RestaurantId;

	//Load the record from the db
	Restaurant restaurant = _restaurantRepository.Load(restaurantId);

	// set the viewmodel name
        viewModel.Name = restaurant.Name
	
	//return the viewmodel back to the browser
	return Json(viewModel);
}

Setting up your view
--------------------

<script type="text/javascript" language="javascript">
	@Html.Action("KnockOutJs","Home")
</script>

What does Knockout creator rendered in the browser
--------------------------------------------------

<script type="text/javascript" language="javascript">
$(function() {var baseModel = {RestaurantId: ko.observable(),
Name: ko.observable(),
$(function() {var  Restaurant = ko.mapping.fromJS(baseModel);
window.Restaurant = Restaurant;
ko.applyBindings(window.Restaurant);Restaurant.Name.subscribe(function(){test()});
});
});
</script>

So what now?
------------
Everytime the RestaurantId is updated a call will be made to the server and will run GetRestaurantById.

