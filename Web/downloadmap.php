<?php 
//include me where you want your accept code to run.
include_once "../WebInclude/funcs.php";
//done until additional map metadata is added.
session_start();
if (isLoggedIn()) {
	$cmrID = getCMRID();
	$mapname = urldecode($_GET['map']);
	$mappath = 'C:\\CMR\\Maps\\' . $cmrID . '\\pending\\' . $mapname;
	if (file_exists($mappath)) {
		session_write_close();
		header("X-Sendfile: $mappath");
		header("Content-type: application/octet-stream");
		header('Content-Disposition: attachment; filename="' . basename($mappath) . '"');
	} else {
		$_SESSION['warning'] = "error, that file doesnt exist. pls.";
		session_write_close();
		header('Location: http://eklipz.us.to/cmr/maptest.php');
	}
} else {
	$_SESSION['redirect'] = "http://eklipz.us.to/cmr/maptest.php";
	$_SESSION['warning'] = "You need to log in before downloading maps.";
	session_write_close();
	header( 'Location: http://eklipz.us.to/cmr/login.php' );
}
?>