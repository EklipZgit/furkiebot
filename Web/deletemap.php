<?php
include_once "../WebInclude/funcs.php";

$maps = getMaps();
$mapname = urldecode($_GET['map']);
$nameLower = strtolower($mapname);
$mapauthor = "";
if (array_key_exists($nameLower, $maps)) {
	$mapauthor = $maps[$nameLower]->author;
}

session_start();
if (isLoggedIn()) {
	if (isTester() || isAdmin() || strtolower($mapauthor) == strtolower($_SESSION['username'])) {
		$cmrID = getCMRID();
		$mappath = 'C:\\CMR\\Maps\\' . $cmrID . '\\' . $mapname;

		if (array_key_exists($nameLower, $maps)) {
			$maps[$nameLower]->accepted = false;
			$maps[$nameLower]->acceptedBy = "";
			unlink($mappath);
			unset($maps[$nameLower]);
			writeMaps($maps);
			
			$_SESSION['message'] = "You successfully deleted " . $mapname;
		} else {
			$_SESSION['warning'] = "error, you tried to delete a file that doesnt exist.";
		}
		session_write_close();
		header('Location: http://eklipz.us.to/cmr/maptest.php');
	} else {
		include "../WebInclude/testeroption.php";
	}
} else {
	$_SESSION['redirect'] = "http://eklipz.us.to/cmr/maptest.php";
	$_SESSION['warning'] = "You need to log in before testing maps.";
	session_write_close();
	header( 'Location: http://eklipz.us.to/cmr/login.php' );
}
?>