<?php
include_once "../WebInclude/funcs.php";
include_once "../WebInclude/MapData.php";

$cmrID = getCMRID();
session_start();
if (isset($_SESSION['loggedIn'])) {
	$temp = explode(".", $_FILES["file"]["name"]);
	$extension = end($temp);

	if ($extension == "html" || $extension == "php"){
		echo '<p style="color: red;"><b>Your account is hereby banned from all future CMR\'s and your racedata deleted.</b></p>Just kidding. Don\'t try to exploit my server though. ♥';
	} else if (count($temp) == 1) { //no file extensions.

		$filename = $_POST["mapname"];
		$mapname = preg_replace('/[\x00-\x1F\x80-\xFF]/', '', $filename); //REGEX's OUT CONTROL CODES AND WHATNOT.
		if ($_FILES["file"]["error"] > 0) {
			echo '<p style="color: red;">ERROR UPLOADING. Return Code: " . $_FILES["file"]["error"] . "<br>';
			echo "screenshot this to EklipZ in #DFcmr</p>";
		} else if (trim($_POST['mapname']) == "" ) {
			$_SESSION['warning'] = "You must submit a map with a proper map name.";
			session_write_close();
			header( 'Location: http://eklipz.us.to/cmr/map.php');
		} else {
			$maps = getMaps();
			
			$mapnameLower = strtolower($mapname);
			$mapper = $_SESSION['usernameCase'];

			$mappath = "C:/CMR/Maps/" . $cmrID . "/" . $mapname;


			//Shit to set up the array for loading into FurkieBot.
			if (isset($maps[$mapnameLower])) {
				$mapdata = $maps[$mapnameLower];
			} else {
				$mapdata = new MapData();
			}
			$mapdata->name = $mapname;
			$mapdata->filepath = $mappath;
			$mapdata->author = $mapper;
			$mapdata->accepted = false;

			$maps[$mapnameLower] = $mapdata;

			if (file_exists($mappath)) {
				unlink($mappath);
				move_uploaded_file($_FILES["file"]["tmp_name"], $mappath);
				$_SESSION['message'] = "Replaced: " . $mapname . " successfully.<br>";
			} else {
				move_uploaded_file($_FILES["file"]["tmp_name"], $mappath);
				$_SESSION['message'] = "Uploaded: " . $mapname . " successfully.<br>";
			}
			writeMaps($maps); //Save the array back to the map file.

			$_SESSION['message'] = $_SESSION['message'] . "Thanks " . $_SESSION['usernameCase'] . "!<br>";
			session_write_close();
			header( 'Location: http://eklipz.us.to/cmr/map.php' );
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