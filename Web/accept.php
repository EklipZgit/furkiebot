<?php 
	//include me where you want your accept code to run.
	include_once "../WebInclude/funcs.php";
	//done until additional map metadata is added.
	
	session_start();
	if (isLoggedIn()) {
		$cmrID = getCMRID();
		$mapname = urldecode($_GET['map']);
		$mappath = 'C:\\CMR\\Maps\\' . $cmrID . '\\pending\\' . $mapname;
		$targetpath = 'C:\\CMR\\Maps\\' . $cmrID . '\\accepted\\' . $mapname;

		if (file_exists($mappath)) {
			rename($mappath, $targetpath);
			$_SESSION['message'] = "You successfully accepted " . $mapname;
		} else {
			$_SESSION['warning'] = "error, you tried to accept a file that doesnt exist.";
		}
		session_write_close();
		header('Location: http://eklipz.us.to/cmr/maptest.php');
	} else {
		$_SESSION['redirect'] = "http://eklipz.us.to/cmr/maptest.php";
		$_SESSION['warning'] = "You need to log in before testing maps.";
		session_write_close();
		header( 'Location: http://eklipz.us.to/cmr/login.php' );
	}
?>