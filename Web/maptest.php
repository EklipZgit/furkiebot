<?php
session_start();
if (isset($_SESSION['loggedIn'])) {
echo <<< EOT
	<html><head><style>
	#main {
		width: 800px;
		text-alight: left;
		align: center;
	}</style></head><body><div id="main">
EOT;
	$userlistfile = "C:\CMR\Data\Userlist\userlistmap.json";
	$filestring = file_get_contents($userlistfile);
	$userarray = json_decode($filestring, true);
	$username = $_SESSION['username'];
	if (array_key_exists($username, $userarray)) {
		$userData = $userarray[$username];
		if ($_SESSION['trusted'] == 1) {
			echo "is trusted<br>";
			if ($_SESSION['tester'] == 1) { //THIS GUY IS A DEDICATED CMR TESTER???
				echo "is tester. This page will house map testing stuff in a little bit.<br>";
				//do shit
			} else { //THIS GUY IS NOT ALLOWED TO DEDI-CMR TEST.
				echo "not set to tester. This page will allow you to set yourself to be a tester in the future, probably. Once I work out PHP communication with FurkieBot.<br>";
				//uh, maybe do other shit
			}
		} else {
			echo '<p>Sorry, you are not allowed to be a map tester. If you feel you ought to be allowed to be a map tester, talk to a CMR Admin in IRC.</p>';
		}
	} else {
		echo '<p style="color:red;"> SOMETHING WENT TERRIBLY, TERRIBLY WRONG. DONT DO ANYTHING AND MESSAGE EKLIPZ IMMEDIATELY DETAILING EXACTLY WHAT YOU DID TO REACH THIS PAGE, WHAT YOU LOGGED IN AS, ETC. TELL HIM "maptest.php reports inconsistent user registration" AND TRY TO STOP HIM FROM SETTING HIMSELF ON FIRE</p>';
	}
} else {
	$_SESSION['redirect'] = "http://eklipz.us.to/cmr/maptest.php";
	$_SESSION['warning'] = "You need to log in before accessing the map testing page.";
	session_write_close();
	header( 'Location: http://eklipz.us.to/cmr/login.php' );
}
echo '</div></body></html>';
?>