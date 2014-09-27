
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
		ul.mr {
			margin-right: 55%;
			font-size: 20pt;
		}
		p.ml {
			margin-left: 1cm;
		}
		h3.ml {
			margin-left: 1cm;
		}
		li.ml {
			margin-left: 1cm;
			padding-bottom: 8px;
			font-size: 11pt;
		}
	</style>

</head>

<body>

	<div class="jumbotron">


		<?php displayNavbar("FAQ"); ?>

		<h2>Custom Map Race FAQ</h2>
		<ul class="mr">
			<li class="ml">
				A Custom Map Race (CMR) is a BLIND SS race of custom maps. 
			</li>
			<li class="ml">
				Mapmakers make these maps during the week or so leading up to the race.
			</li>
			<li class="ml">
				Every Saturday the mappers whose maps have been accepted by the maptester(s) upload their maps to Atlas right before the race. The racers then download the maps, and wait for FurkieBot to give the word to start the race!
			</li>
		</ul>
	</div>

	<!-- Example row of columns -->
	<div class="row">
		<div class="col-md-4">
			<h3 class="ml">IRC</h3>
			<p class="ml">
				The race is held in the SpeedrunsLive IRC server, in a custom channel that FurkieBot will create when preparing to start the race.
			</p>
			<p class="ml">
				<a class="btn btn-default" href="http://client01.chat.mibbit.com/#dustforce@irc2.speedrunslive.com" role="button">IRC &raquo;</a>
			</p>
		</div>
		<div class="col-md-4">
			<h3 class="ml">Maps</h3>
			<p class="ml">
				<li class="ml">
				Anybody can make a CMR map! By making maps for the CMR, you get the advantage of knowing the maps you made ahead of time for the race! <b><a href="testing.php">You must first read the Map Making rules carefully</a></b>, however. Your map will not be accepted if you do not follow the rules!
				</li>

				<li class="ml">
					You can either be there to upload your map to atlas when the race starts, or FurkieBot will publish your map for you (and give you credit) if you are not in the IRC channel within 10 minutes of the designated race time (20 minutes after the time listed).
				</li>
				<li class="ml">
				You just need to register with FurkieBot <a href="http://eklipz.us.to/cmr/register.php">(More info here)</a> and then use the Map submission form on this site to submit your map!
				</li>
				<li class="ml">
				Maps do not need to be decorated before submission, but the gameplay should be done. When decorating after submission there can be severe penalties if you:
				<li class="ml">Make parts of the map too dark to see</li>
				<li class="ml">Zoom the camera out further than it was in the submission</li>
				<li class="ml">Change the color scheme so that dust, enemies, or the player are harder to see</li>
				<li class="ml">Add things to layer 20 that obscure dust, the player, or make it hard to tell what parts of the map the player is able to touch</li>
				</li>
			</p>
			<p class="ml">
				<a class="btn btn-default" href="map.php" role="button">Submit a map &raquo;</a>
			</p>
		</div>

	</div>
</body>
</html>
<!-- don't look -->