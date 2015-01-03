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
			<?php include "../WebInclude/displaymessages.php"; ?>

			<div id="main">
				<h3>
					<b>Upload a map!</b>
				</h3>

				<form action="mapsubmit.php" method="post" enctype="multipart/form-data">
					<table>
						<tr>
							<td><label for="file">Map File:</label></td>
							<td><input type="file" name="file" id="file"></td>
						</tr>
						<tr>
							<td><label for="mapname">Map Name:</label></td>
							<td><input type="text" name="mapname" id="mapname" required></td>
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



				<?php include "../WebInclude/maprules.php"; ?>
				
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
	session_write_close();
} else {
	$_SESSION['redirect'] = "map.php";
	$_SESSION['warning'] = "You need to log in before uploading maps.";
	session_write_close();
	header( 'Location: login.php' );
}
?>

