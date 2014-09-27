<!DOCTYPE html>
<?php include "../WebInclude/navbar.php"; // YOU NEED TO INCLUDE THIS FILE AT THE TOP OF THE PAGE.
?> 

<html lang="en">
<head>
	<title>Participate in a CMR!</title>
	<!-- Bootstrap core CSS -->
	<link href="css/bootstrap.min.css" rel="stylesheet">
	<!-- Custom styles for this template -->
	<link href="css/starter-template.css" rel="stylesheet">
	<style>
		#main {
			width: 800px;
			text-align: justify;
		}
		li {
			padding-bottom: 10px;
		}
	</style>
</head>
<body>
	<div class="skinny">
		<?php displayNavbar("Participate"); ?>
		<div id="main">
			<h3>How to participate in a CMR: </h3>
			<ul>
				<li class="partlist">
					In order to participate in a CMR you will need to join our IRC channel #dustforce on the speedrunslive irc server. (irc2.speedrunslive.com:6667). You can do this in your web browser with this link: <a href="http://client01.chat.mibbit.com/#dustforce@irc2.speedrunslive.com">Mibbit connection</a>
				</li>
				<li class="partlist">
					In order to improve the system and add in cool features like rankings, acceptance / map tracking via the website, etc, I've needed to add a layer of security into CMR's. This means that you must be using a Registered Nickname on the SpeedRunsLive IRC server, and then also register with FurkieBot on the IRC server. Dont worry, its easy!
				</li>
				<li class="partlist">
					<b>Skip this step if you already have a nick registered on SRL.</b> <br>All you need to do is "<b>/nick theNicknameYouWant</b>" to select an unregistered nickname that you will use for all races in the future. Then, "<b>/msg NickServ REGISTER password emailAddress</b>". Because IRC messages are sent in plaintext, there is a risk that passwords sent via IRC could be intercepted, so <b>don't use the same password as you use for important things!</b> In the future, you will need to use this password to identify yourself to nickserv, so dont forget your password!
				</li>
				<li class="partlist">
					Then, once you are "Identified" with NickServ, you will need to register with FurkieBot. "<b>/msg FurkieBot REGISTER password</b>" where your password does NOT need to be the same as your SRL password. This is the password you will use to log into the CMR site with, for things like map submissions, map testing, and any other features that will have anything to do with your specific account. This password is easy to change if you forget it, unlike your SRL password. And dont worry, your passwords are not stored by the CMR website nor by furkiebot. They are never printed to the console, and are hashed with SHA256 encryption, salted, and rehashed before being stored. The code is publicly available for you to see how passwords are encrypted at <a href="https://github.com/EklipZgit/furkiebot"> https://github.com/EklipZgit/furkiebot</a>
				</li>
				<li class="partlist">
					Next, you will need to show up in IRC before the race! Be prepared to wait near your computer for a bit while other racers get ready. If you submitted a map that was accepted for the race, you will publish your (unmodified) maps with the tag custom-map-race-(racenumber) and the same name you submitted them with. Remember to thank the mapmakers! Without them, CMR's would not be possible!
				</li>
				<li class="partlist"> 
					FurkieBot will create a racechannel which you will join, and remind you to set your in-game-name to the name you use on the dustforce leaderboard, so that FurkieBot can check to make sure you SS'd all of the maps when you .done. Dont forget to .setign! those seconds at the end of the race to fix your IGN may prove crucial!
				</li>
				We will be trying to hold a race every week, so be prepared!
			</div>
		</div>
	</body>
	</html>
