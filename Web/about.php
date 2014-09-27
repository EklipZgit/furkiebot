
<!DOCTYPE html>
<?php include "../WebInclude/navbar.php"; // YOU NEED TO INCLUDE THIS FILE AT THE TOP OF THE PAGE.
?> 
<html lang="en">
<head>

	<title>CMR Information</title>

	<!-- Bootstrap core CSS -->
	<link href="css/bootstrap.min.css" rel="stylesheet">

	<!-- Navbar CSS -->
	<link href="css/navbar.css" rel="stylesheet">

	<style>
		p.mr {
			margin-right: 55%;
		}
	</style>

</head>

<body>

	<div class="jumbotron">


		<?php displayNavbar("FAQ");  ?>
		<h1>Custom Map Races</h1>
		<p class="mr">
			A Custom Map Race (CMR) is a BLIND SS race of custom maps. 
			<br>Mapmakers make these maps during the week or so leading up to the race.<br>
			Every Saturday the mappers whose maps have been accepted by the maptester(s) upload their maps to Atlas right before the race, and the racers download the maps after clearing their Custom Maps folder to make finding the maps quicker.
		</p>
	</div>

	<!-- Example row of columns -->
	<div class="row">
		<div class="col-md-4">
			<h2>IRC</h2>
			<p>Donec id elit non mi porta gravida at eget metus. Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo sit amet risus. Etiam porta sem malesuada magna mollis euismod. Donec sed odio dui. </p>
			<p><a class="btn btn-default" href="http://client01.chat.mibbit.com/#dustforce@irc2.speedrunslive.com" role="button">IRC &raquo;</a></p>
		</div>
		<div class="col-md-4">
			<h2>Heading</h2>
			<p>Donec id elit non mi porta gravida at eget metus. Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo sit amet risus. Etiam porta sem malesuada magna mollis euismod. Donec sed odio dui. </p>
			<p><a class="btn btn-default" href="#" role="button">View details &raquo;</a></p>
		</div>
		<div class="col-md-4">
			<h2>Heading</h2>
			<p>Donec sed odio dui. Cras justo odio, dapibus ac facilisis in, egestas eget quam. Vestibulum id ligula porta felis euismod semper. Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo sit amet risus.</p>
			<p><a class="btn btn-default" href="#" role="button">View details &raquo;</a></p>
		</div>

	</div>	<!-- was missing -->
</body>
</html>
