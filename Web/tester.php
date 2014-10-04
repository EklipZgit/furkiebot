<!DOCTYPE html>
<?php include "../WebInclude/navbar.php"; // YOU NEED TO INCLUDE THIS FILE AT THE TOP OF THE PAGE.
?> 

<html lang="en">
<head>
	<title>Map test information!</title>
	<!-- Bootstrap core CSS -->
	<link href="css/bootstrap.min.css" rel="stylesheet">
	<!-- Custom styles for this template -->
	<link href="css/starter-template.css" rel="stylesheet">
	<style>
		#main {
			width: 800px;
			text-align: left;
		}
	</style>

</head>

<body>
	<div class="skinny">
		<?php displayNavbar("Map requirements"); ?>
		<div id="main">
			<b>Temporary rules for being a map tester:</b><br><br>
			<b>You MUST place the maps in your custom map folder to test, AND MAKE SURE YOU SEE S COMPLETION AT THE END OF THE MAP.</b><br>
			Steam: &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp"C:\Program Files (x86)\Steam\SteamApps\common\Dustforce\user\levels"<br>
			DRM Free: "C:\Users\USER_NAME\AppData\Roaming\Dustforce\user\levels" for DRM free version.<br>
			<br><br>
			<?php include "../WebInclude/maprules.php"; ?>
			<br>
			For the time being, Testers should notify the Map Maker via <a href="http://client01.chat.mibbit.com/#dustforce@irc2.speedrunslive.com">IRC</a> or steam after testing their map, letting them know it was accepted<br>
			or why it was denied. Testers should then notify channel <a href="http://client01.chat.mibbit.com/#DFcmr@irc2.speedrunslive.com">#DFcmr.</a> that you have tested and confirmed that the map meets the above requirements. <br>
			This will eventually become automated, but for now it must be done manually.
			<br><br>
			If you feel that any requirements are missing from the above rules, let EklipZ know in #DFcmr.<br><br><br>
			<b> Do you want to become a dedicated tester? For now, talk to EklipZ in <a href="http://client01.chat.mibbit.com/#DFcmr@irc2.speedrunslive.com">#DFcmr.</a></b>
		</div>
	</div>
</body>
</html>

