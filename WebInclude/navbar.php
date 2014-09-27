
<?php
	$ek = "http://eklipz.us.to/cmr/";

	//THIS IS WHERE YOU ADD NEW PAGES. They will auto add to the sidebar.
	$navlist = [
	"Home" => "${ek}index.html",
	"FAQ" => "${ek}about.html",
	"Upload map" => "${ek}map.php",
	"Testers" => "${ek}maptest.php",
	//    "Home" => "${ek}index.html",      //add more as more shit gets added to site
	];



	function displayNavbar($currentpage) {

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
								echo '<li class="active">' . $key . '</li>';
							} else {
								echo '<li class="inactive"><a href="' . $value . '">' . $key . '</a></li>';
							}
						}

					?>
					
				</ul>
			</div><!--/.nav-collapse -->
</div>

<?php
	}
?>