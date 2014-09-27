<!DOCTYPE html>
<?php include "../WebInclude/navbar.php"; // YOU NEED TO INCLUDE THIS FILE AT THE TOP OF THE PAGE.
?> 

<html lang="en">
<head>
	<title>NAVBAR DEMO</title>
	<!-- Bootstrap core CSS -->
	<link href="css/bootstrap.min.css" rel="stylesheet">
	<!-- Custom styles for this template -->
	<link href="css/starter-template.css" rel="stylesheet">
	<style>
		#main {
			width: 800px;
			text-alight: left;
			align: center;
		}
	</style>
</head>
<body>
<?php 			// CALL THIS METHOD LIKE SO AND PASS IN THE NAME (as displayed on the navbar) OF THE CURRENT PAGE.
displayNavbar("Home");      
?>
<a href="index.php"> <b> Home</b></a><br><br>
<div id="main">
	<p>To register you will need to join our IRC channel #dustforce on the speedrunslive irc server. (irc2.speedrunslive.com:6667). You can do this in your web browser with this link: <a href="http://client01.chat.mibbit.com/#dustforce@irc2.speedrunslive.com">#Dustforce on Mibbit</a></p>

	<p>All you need to do is 
		<br><br><b>/nick &nbsp theNicknameYouWant</b>
		<br><br> to select a nickname that you will use to log into this site, and for races in IRC. Then, 
		<br><br><b>/msg &nbsp NickServ &nbsp REGISTER &nbsp password &nbsp emailAddress</b> &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp*** See warning below
		<br><br> Because IRC messages are sent in plaintext, there is a risk that passwords sent via IRC could be intercepted, so <b>don't use the same password as you use for important things!</b> In the future, you will need to give NickServ that password in order to identify yourself to continue using this nickname in the future, so don't forget your password!</p>
		<p>Then, once you are "Identified" with NickServ, you will need to register with FurkieBot. 
			<br><br><b>/msg &nbsp FurkieBot &nbsp REGISTER &nbsp password</b> &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp*** See warning below
			<br><br> where your password does NOT need to be the same as your SRL password. This is the password you will use to log into the CMR site with, for things like map submissions, map testing, and any other features that will have anything to do with your specific account. This password is easy to change if you forget it, unlike your SRL password. And don't worry, your passwords are not stored by the CMR website nor by furkiebot. They are never printed to the console, and are hashed with SHA256 encryption, salted, and rehashed before being stored. The code is publicly available for you to see how passwords are encrypted at <a href="https://github.com/EklipZgit/furkiebot"> https://github.com/EklipZgit/furkiebot</a></p>

			<p><b>*** &nbsp&nbsp"/msg" is the way to private message in most clients, including Mibbit, mIRC and Hexchat.</b> If you are using another client, it may be /query or something else, check first if you aren't using one of the above 3 so that you dont accidentally broadcast your password to a channel. You may also join an empty channel if you desire with "/join #somethingrandom" so that anything accidentally sent goes there.</p>
		</div>
	</body>
	</html>

