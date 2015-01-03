<?php 
include_once "../WebInclude/funcs.php";
//done until additional map metadata is added.
ensureSession();
if (isLoggedIn()) {
	if (isTester() || isAdmin()) {
		$cmrID = getCMRID();
		$mapname = trim(urldecode($_GET['map']));
		$mappath = 'C:/CMR/Maps/' . $cmrID . '/' . $mapname;
		if (file_exists($mappath)) {


			header("X-Sendfile: $mappath");
			header("Content-type: application/octet-stream");
			header('Content-Disposition: attachment; filename="' . basename($mappath) . '"');
			readfile($mappath);
			session_write_close();
			exit;
		} else {
			$_SESSION['warning'] = "error, that file doesnt exist. pls.";
			session_write_close();
			header('Location: maptest.php');
			exit;
		}
	} else {		
		include "../WebInclude/testeroption.php";
	}
} else {
	$_SESSION['redirect'] = "maptest.php";
	$_SESSION['warning'] = "You need to log in before downloading maps.";
	session_write_close();
	header( 'Location: login.php' );
	exit;
}
?>