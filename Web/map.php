<?php
include "../WebInclude/funcs.php";
include "../WebInclude/navbar.php"; // YOU NEED TO INCLUDE THIS FILE AT THE TOP OF THE PAGE.


session_start();
if (isLoggedIn()) {
	?>


	<html lang="en">
	<head>
		<title>Submit a Map</title>
		<!-- Bootstrap core CSS -->
		<link href="css/bootstrap.min.css" rel="stylesheet">
		<!-- Custom styles for this template -->
		<link href="css/starter-template.css" rel="stylesheet">
		<style>
			div.skinny {
				text-align: left;
				padding-left: 15px;
			}
			div#main {
				width: 800px;
				text-alight: center;
			}
			label {
				display: inline-block;
				width: 200;
			}
			input {
				display: inline-block;
				width: 500;
			}
		</style>
	</head>
	<body>
		<div class="skinny">
			<?php displayNavbar("Upload map"); ?>
			<b>Upload a map!</b>
			<div id="main">
				<form action="mapsubmit.php" method="post" enctype="multipart/form-data">
					<table>
						<tr>
							<td><label for="file">Map File:</label></td>
							<td><input type="file" name="file" id="file"></td>
						</tr>
						<tr>
							<td><label for="mapname">Map Name:</label></td>
							<td><input type="text" name="mapname" id="mapname"></td>
						</tr>
						<tr>
							<td>&nbsp</td>
							<td><input type="submit" name="submit" value="Submit" id="submitbutton"></td>
						</tr>
					</table>
				</form>



				YOU MAY NOT LET ANYONE PLAYTEST YOUR CMR MAPS AT ANY STAGE IN DEVELOPMENT EXCEPT DESIGNATED TESTERS.<br>
				THE FOCUS OF CMR'S IS ON THE <i>BLIND</i> ASPECT OF RACING; IT IS IMPORTANT THAT NOBODY RACING<br>
				HAS PREPLAYED YOUR MAP, OR IS PREPARED FOR IT IN ANY WAY!<br>



				<br>
				Make sure you submit with the proper map name and username.<br>
				If you need to re-submit your map, MAKE SURE to use the same map name and username that you did the first time you uploaded it.<br><br>



				<b>Temporary rules for maps:</b><br>
				A map must have basic playability (decent camera, ensure it is SSable, visible color scheme, map contrasts with player and dust color, etc).<br>
				Maps may not be harder than Gold Key levels.<br>
				Maps may not contain blind drops where you fall into a place that requires precise input.<br>
				&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp(As a rule of thumb, if you die more than once there because you werent prepared, its not ok).<br>
				The goal flag should be properly linked to the end enemies. <br>
				&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbspThe enemies linked to the goal flag should be relatively obvious so that racers do not accidentally end the map early.<br>
				Maps may not use a zoom level over 1400, due to lag experienced by many players. <br> 
				&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbspWhether you lag or not, the camera must be less zoomed out than Abyss. Also, no over-use of props.<br>
				No slopes that lead directly into spikes.<br> 
				Maps must have reasonable checkpoint intervals. (Checkpoints at least every 15 seconds of gameplay is a good rule of thumb, but can be waived for shorter maps).<br>
				If a map is character specific, it must include that character's abbreviation (Man, Girl, Kid, Worth) at the start of the map name. <br>
				&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbspThis <b>includes</b> when you publish it on atlas for the race. 
				<br><br>
				MAPS MAY NOT BE DENIED BASED ON "ENJOYABILITY" OF THE MAP OR ANY OTHER SUBJECTIVE FACTORS.<br>
				PROVIDED IT MEETS THE ABOVE REQUIREMENTS, A MAP IS ALLOWED.<br>
				<br>
				With that said, here are a few tips for keeping racers happy.<br>
				Place your checkpoint flags flat on the ground. Spawning in the air sucks.<br>
				Ensure your dust color contrasts well with the ground / background. <br>
				Try to avoid gimicky maps that take too long to learn the gimick.<br>
			</div>
		</div>
	</body>
	</html>	

	<?php 
} else {
	$_SESSION['redirect'] = "http://eklipz.us.to/cmr/map.php";
	$_SESSION['warning'] = "You need to log in before uploading maps.";
	session_write_close();
	header( 'Location: http://eklipz.us.to/cmr/login.php' );
}
session_write_close();
?>

