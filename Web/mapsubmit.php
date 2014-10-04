<?php
include "../WebInclude/funcs.php";
$cmrID = getCMRID();
session_start();
if (isset($_SESSION['loggedIn'])) {
	$temp = explode(".", $_FILES["file"]["name"]);
	$extension = end($temp);

	if ($extension == "html" || $extension == "php"){
		echo '<p style="color: red;"><b>Your account is hereby banned from all future CMR\'s and your racedata deleted.</b></p>Just kidding. Don\'t try to exploit my server though. ♥';
	} else if (count($temp) == 1) { //no file extensions.
		echo "Thanks " . $_SESSION['usernameCase'] . "!<br>";
		if ($_FILES["file"]["error"] > 0) {
			echo "ERROR UPLOADING. Return Code: " . $_FILES["file"]["error"] . "<br>";
			echo "screenshot this to EklipZ in #DFcmr";
		} else if ($_POST['mapname'] == "") {
			echo "ERROR. You need to submit a map along with an actual name.";
		} else {
			$maps = getMaps();
			$filename = $_SESSION['usernameCase'] . "-" . $_POST["mapname"];
			$safefile = preg_replace('/[\x00-\x1F\x80-\xFF]/', '', $filename); //REGEX's OUT CONTROL CODES AND WHATNOT.
			
			$mapnameLower = strtolower($mapname);
			$mapper = $_SESSION['usernameCase'];

			$mappath = "C:/CMR/Maps/" . $cmrID . "/" . $mapname;

			if (isset($maps[$mapnameLower])) {
				$mapdata = $maps[$mapnameLower];
			} else {
				$mapdata = array();
				$mapdata['id'] = -1;
				$mapdata['acceptedBy'] = "";
			}
			
			//Shit to set up the array for loading into FurkieBot.
			$mapdata['name'] = $mapname;
			$mapdata['filepath'] = $mappath;
			$mapdata['author'] = $mapper;
			$mapdata['accepted'] = false;
			$maps[$mapnameLower] = $mapdata;
			if (file_exists($mappath)) {
				unlink($mappath);
				move_uploaded_file($_FILES["file"]["tmp_name"], $mappath);
				echo "Replaced: " . $mapname . " successfully.<br>";
			} else {
				move_uploaded_file($_FILES["file"]["tmp_name"], $mappath);
				echo "Uploaded: " . $mapname . " successfully.<br>";
			}
			writeMaps($maps); //Save the array back to the map file.
		}
	} else {
		echo "uh oh you had a \".\" in the filename. Please, no files with extensions or \".\"'s";
		echo "<br><a href=\"map.php\">Resubmit</a>";
	}
} else {
	$_SESSION['redirect'] = "http://eklipz.us.to/cmr/map.php";
	$_SESSION['warning'] = "You need to log in before uploading maps.";
	session_write_close();
	header( 'Location: http://eklipz.us.to/cmr/login.php' );
}
?>