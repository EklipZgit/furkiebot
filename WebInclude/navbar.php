
<?php
include_once "funcs.php";
function displayNavbar($currentpage) {
	$ek = "";

	//THIS IS WHERE YOU ADD NEW PAGES. They will auto add to the sidebar.
	$navlist = [
	"FAQ" => "${ek}about.php",
	"Upload map" => "${ek}map.php",
	"Map Testers" => "${ek}maptest.php",
	"Map Rules" => "${ek}tester.php",
	//    "Home" => "${ek}index.html",      //add more as more shit gets added to site
	];
	ensureSession();
	if (isLoggedIn()) {		
		$navlist['Log out!'] = "${ek}logout.php";					
	} else {
		$navlist['Log in!'] = "${ek}login.php";
		$navlist['Register?'] = "${ek}register.php";
	}
	?>

	<div class="navbar navbar-inverse navbar-fixed-top" role="navigation">
		<div class="navbar-header">
			<button type="button" class="navbar-toggle collapsed" data-toggle="collapse" data-target=".navbar-collapse">
				<span class="sr-only">Toggle navigation</span>
				<span class="icon-bar"></span>
				<span class="icon-bar"></span>
				<span class="icon-bar"></span>
			</button>
			<?php echo "<a href=\"${ek}index.php\" class=\"navbar-brand\">FurkieBot Central</a>"; ?>
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
				?>
				
			</ul>
		</div><!--/.nav-collapse -->
	</div>


	<?php
}
?>