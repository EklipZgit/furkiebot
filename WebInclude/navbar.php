
<?php
include_once "funcs.php";
function displayNavbar($currentpage) {

	$ek = "http://eklipz.us.to/cmr/";

	//THIS IS WHERE YOU ADD NEW PAGES. They will auto add to the sidebar.
	$navlist = [
	"Home" => "${ek}index.php",
	"FAQ" => "${ek}about.php",
	"Upload map" => "${ek}map.php",
	"Map Testers" => "${ek}maptest.php",
	//    "Home" => "${ek}index.html",      //add more as more shit gets added to site
	];

	?>

	<div class="navbar navbar-inverse navbar-fixed-top" role="navigation">
		<div class="navbar-header">
			<button type="button" class="navbar-toggle collapsed" data-toggle="collapse" data-target=".navbar-collapse">
				<span class="sr-only">Toggle navigation</span>
				<span class="icon-bar"></span>
				<span class="icon-bar"></span>
				<span class="icon-bar"></span>
			</button>
			<a class="navbar-brand">Custom Map Races</a>
		</div>

		<div class="collapse navbar-collapse">
			<ul class="nav navbar-nav">

				<?php 

					foreach ($navlist as $key => $value) {
						if ($key == $currentpage) {
							echo '<li class="active"><a href="' . $value . '">' . $key . '</a></li>';			//THE CURRENTLY SELECTED PAGES NAVBAR BUTTON
						} else {
							echo '<li class="inactive"><a href="' . $value . '">' . $key . '</a></li>';			//ALL OTHER PAGES NAVBAR BUTTONS
						}
					}
					if (isLoggedIn()) {
							echo '<li class="inactive login"><a href="' . $ek . "login.php" . '">Log In!</a></li>';	
							echo '<li class="inactive register"><a href="' . $ek . "register.php" . '">Register?</a></li>';							
					} else {
							echo '<li class="inactive logout"><a href="' . $ek . "logout.php" . '">Log out</a></li>';	
					}

				?>
				
			</ul>
		</div><!--/.nav-collapse -->
	</div>


	<?php
}
?>